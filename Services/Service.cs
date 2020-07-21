using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiTools.Context;
using ApiTools.Helpers;
using ApiTools.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Services
{
    public interface IService<TModel, TModelKeyId, TModelDto>
        where TModel : IContextEntity<TModelKeyId>
        where TModelKeyId : new()
        where TModelDto : IDtoModel<TModelKeyId>
    {
        Task<IServiceResponse<TModelDto>> Create(TModel model);
        Task<IServiceResponse<IEnumerable<TModelDto>>> Create(IEnumerable<TModel> models);

        Task<IServiceResponse<TModelDto>> Read(
            TModelKeyId id,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        );

        Task<IServiceResponse<IEnumerable<TModelDto>>> Read(
            Expression<Func<TModel, bool>> expression,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        );

        Task<IServiceResponse<TModelDto>> ReadOne(
            Expression<Func<TModel, bool>> expression,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        );


        Task<IServiceResponse<IEnumerable<TModelDto>>> Read(
            IEnumerable<TModelKeyId> ids,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        );

        Task<IServiceResponse<PagingServiceResponse<TModelDto>>> Read(
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        );

        // Task<IServiceResponse<object>> Read(
        //     TModelKeyId id,
        //     string selectField,
        //     ServiceOptions<TModel> options = default,
        //     ContextOptions readOptions = default
        // );
        //
        // Task<IServiceResponse<object>> Read(
        //     string selectField,
        //     ServiceOptions<TModel> options = default,
        //     ContextOptions readOptions = default
        // );

        // Task<IServiceResponse<object>> Read(
        //     TModelKeyId id,
        //     string selectField,
        //     string fieldId,
        //     ServiceOptions<TModel> options = default,
        //     ContextOptions readOptions = default
        // );

        Task<IServiceResponse> Delete(TModelKeyId id);
        Task<IServiceResponse> Delete(IEnumerable<TModelKeyId> ids);


        Task<IServiceResponse> Update(TModel model);
        Task<IServiceResponse> Update(TModelDto model);


        Task<IServiceResponse<TModelDto>> Create(TModelDto data);

        Task<IServiceResponse<IEnumerable<TModelDto>>> Create(
            IEnumerable<TModelDto> dataModels,
            ServiceOptions<TModel> options = default
        );

        Task<IServiceResponse> Update(TModelKeyId id, TModelDto data);
        Task<IServiceResponse> Update(IReadOnlyList<BulkUpdateModel<TModelDto, TModelKeyId>> bulkUpdateModels);
    }

    public abstract partial class
        Service<TModel, TModelKeyId, TContext, TModelDto> : IService<TModel, TModelKeyId, TModelDto>
        where TContext : IContext<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelDto : class, IDtoModel<TModelKeyId>
        where TModelKeyId : new()
    {
        protected readonly IHttpContextAccessor Accessor;
        protected readonly IAuthorizationService Authorization;
        protected readonly TContext Context;
        protected readonly IMapper Mapper;
        protected readonly IMapperHelper MapperHelper;
        protected readonly IPagingService PagingService;
        protected readonly ISort Sort;

        protected Service(IServiceHelper<TModel, TModelKeyId> serviceHelper)
        {
            Context = (TContext) serviceHelper.Context;
            Authorization = serviceHelper.Authorization;
            Accessor = serviceHelper.Accessor;
            PagingService = serviceHelper.PagingService;
            Sort = serviceHelper.Sort;
            Mapper = serviceHelper.Mapper;
            MapperHelper = serviceHelper.MapperHelper;
        }

        protected virtual int MaxBulkLimit { get; set; } = 1000;


        public virtual async Task<IServiceResponse> Delete(TModelKeyId id)
        {
            var entity = await Context.FindOne(id);
            if (entity == null) return IServiceResponse.NotFound;

            if (!await AuthorizeDelete(entity)) return IServiceResponse.Forbidden;

            await PreDelete(id);
            await Context.Delete(entity);

            return new ServiceResponse
            {
                StatusCode = StatusCodes.Status204NoContent,
                Success = true
            };
        }


        public virtual async Task<IServiceResponse> Delete(IEnumerable<TModelKeyId> ids)
        {
            var enumerable = ids as TModelKeyId[] ?? ids.ToArray();
            var deleteSet = Context.FindByIds(enumerable);
            var deleteList = await deleteSet.ToListAsync();

            var authorizedEntities = new List<TModel>();
            foreach (var contextEntity in deleteList)
                if (await AuthorizeDelete(contextEntity))
                    authorizedEntities.Add(contextEntity);

            foreach (var entity in authorizedEntities) await PreDelete(entity.Id);
            await Context.Delete(deleteSet);
            return new ServiceResponse
            {
                StatusCode = StatusCodes.Status204NoContent,
                Success = true
            };
        }

        public virtual async Task<IServiceResponse<PagingServiceResponse<TModelDto>>> Read(
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            var query = await ApplyReadOptions(Context.Find(readOptions), options);

            if (query == null) return IServiceResponse<PagingServiceResponse<TModelDto>>.Forbidden;

            var response = await _readWithPaging(SelectDto(query));

            return new ServiceResponse<PagingServiceResponse<TModelDto>>
            {
                Success = true,
                Response = response,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public virtual async Task<IServiceResponse<TModelDto>> Read(
            TModelKeyId id,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        )
        {
            var query = await ApplyReadOptions(Context.Find(Context.FindById(id), readOptions), options);

            if (query == null) return IServiceResponse<TModelDto>.Forbidden;

            var model = await SelectDto(query).SingleOrDefaultAsync();
            if (model == null) return IServiceResponse<TModelDto>.NotFound;

            if (!await AuthorizeRead(model)) return IServiceResponse<TModelDto>.Forbidden;

            return new ServiceResponse<TModelDto>
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Response = model
            };
        }

        public async Task<IServiceResponse<IEnumerable<TModelDto>>> Read(
            Expression<Func<TModel, bool>> expression,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        )
        {
            var query = await ApplyReadOptions(Context.Find(expression, readOptions), options);

            if (query == null) return IServiceResponse<IEnumerable<TModelDto>>.Forbidden;

            var model = await SelectDto(query).ToListAsync();

            return new ServiceResponse<IEnumerable<TModelDto>>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<IServiceResponse<TModelDto>> ReadOne(
            Expression<Func<TModel, bool>> expression,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default
        )
        {
            var query = await ApplyReadOptions(Context.Find(expression, readOptions), options);

            if (query == null) return IServiceResponse<TModelDto>.Forbidden;

            var model = await SelectDto(query).FirstOrDefaultAsync();
            if (model == null) return IServiceResponse<TModelDto>.NotFound;

            if (!await AuthorizeRead(model)) return IServiceResponse<TModelDto>.Forbidden;

            return new ServiceResponse<TModelDto>
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Response = model
            };
        }

        public virtual async Task<IServiceResponse<IEnumerable<TModelDto>>> Read(IEnumerable<TModelKeyId> ids,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            var query = await ApplyReadOptions(Context.FindByIds(ids, readOptions), options);
            if (query == null) return IServiceResponse<IEnumerable<TModelDto>>.Forbidden;

            var models = await SelectDto(query).ToListAsync();

            foreach (var model in MapperHelper.MapDto<TModel, TModelDto>(models))
                if (!await AuthorizeRead(model))
                    return IServiceResponse<IEnumerable<TModelDto>>.Forbidden;

            return new ServiceResponse<IEnumerable<TModelDto>>
            {
                Success = true,
                Response = models,
                StatusCode = StatusCodes.Status200OK
            };
        }


        public virtual async Task<IServiceResponse> Update(TModel data)
        {
            var response = await UpdateWithoutSave(data);
            if (!response.Success) return response;
            await Context.Save();
            var postUpdateResp = await PostUpdate(data);
            if (postUpdateResp.TriggerSave)
                await Context.Save();

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }
        public virtual async Task<IServiceResponse> Update(TModelDto data)
        {
            return await Update(Mapper.Map<TModelDto, TModel>(data));
        }
        

        public virtual async Task<IServiceResponse> Update(TModelKeyId id, TModelDto data)
        {
            var model = await Context.FindOne(id);
            if (!await AuthorizeUpdate(model)) return IServiceResponse.Forbidden;

            var validationResponse = await ValidateData(data, Operations.Update);
            if (!validationResponse.Success) return validationResponse.ToOtherResponse<TModel>();

            var updateModelResponse = await UpdateModel(model, data);
            if (updateModelResponse.Response == null || updateModelResponse.Success == false)
                return updateModelResponse;

            var updateResponse = await Update(updateModelResponse.Response);
            if (updateResponse.Success == false) return updateResponse;
            updateModelResponse.StatusCode = updateResponse.StatusCode;
            var updateRelationResponse = await UpdateRelationData(updateModelResponse, data);
            if (updateRelationResponse.TriggerSave) await Context.Save();
            return updateRelationResponse;
        }

        public virtual async Task<IServiceResponse> Update(
            IReadOnlyList<BulkUpdateModel<TModelDto, TModelKeyId>> bulkUpdateModels)
        {
            // var modelsReadResponse = await Read(bulkUpdateModels.Select(x => x.Id));
            // if (!modelsReadResponse.Success) return modelsReadResponse;
            // var models = modelsReadResponse.Response.ToList();
            var models = await Context.FindByIds(bulkUpdateModels.Select(x => x.Entity.Id)).ToListAsync();


            var validationResponse = await ValidateData(bulkUpdateModels.Select(x => x.Entity), Operations.Update);
            if (!validationResponse.Success) return validationResponse.ToOtherResponse<TModel>();

            var updateData = new List<(IServiceResponse<TModel>, TModelDto)>();
            for (var i = 0; i < models.Count; i++)
            {
                var model = models[i];
                var modelData = bulkUpdateModels.FirstOrDefault(x => x.Entity.Id.Equals(model.Id))?.Entity;
                if (modelData == null) return IServiceResponse.BadRequest;
                var updateModelResponse = await UpdateModel(model, modelData);
                if (!updateModelResponse.Success) return updateModelResponse;
                var updateResponse = await UpdateWithoutSave(updateModelResponse.Response);
                if (!updateResponse.Success) return updateResponse;
                updateData.Add((updateModelResponse, modelData));


                if ((i + 1) % MaxBulkLimit != 0 && i + 1 != models.Count) continue;
                // if this phase is complete then add to database, then clear from memory
                var response = await BulkUpdateOperation(updateData);
                // if operation has failed then return an error response
                if (!response.Success) return response;
                // clear list data
                updateData.Clear();
            }

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        public virtual async Task<IServiceResponse<TModelDto>> Create(TModel data)
        {
            var response = await _Create(data);
            return MapperHelper.MapDto<TModelDto, TModel>(response);
        }

        public async Task<IServiceResponse<IEnumerable<TModelDto>>> Create(IEnumerable<TModel> models)
        {
            var listModels = models.ToList();

            for (var i = 0; i < listModels.Count; i++)
            {
                var model = listModels[i];
                if (!await AuthorizeCreate(model))
                    return IServiceResponse<IEnumerable<TModelDto>>.Forbidden;

                if ((i + 1) % MaxBulkLimit != 0 && i + 1 != listModels.Count) continue;
                await AfterBulkCreate(listModels.Skip(MaxBulkLimit * ((i + 1) / MaxBulkLimit)).Take(i));
            }


            return new ServiceResponse<IEnumerable<TModelDto>>
            {
                Response = MapperHelper.MapDto<TModelDto, TModel>(listModels),
                Success = true,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public virtual async Task<IServiceResponse<IEnumerable<TModelDto>>> Create(IEnumerable<TModelDto> dataModels,
            ServiceOptions<TModel> options)
        {
            var modelDataList = dataModels.ToList();

            var validationResponse = await ValidateData(modelDataList, Operations.Create);
            if (!validationResponse.Success) return validationResponse.ToOtherResponse<IEnumerable<TModelDto>>();


            var entities = new List<TModel>();
            var data = new List<(IServiceResponse<TModel>, TModelDto)>();
            // foreach (var modelData in modelDataList)
            for (var i = 0; i < modelDataList.Count; i++)
            {
                var modelData = modelDataList[i];
                var modelDataResp = await PrepareCreate(modelData);
                if (!modelDataResp.Success)
                    return modelDataResp.ToOtherResponse<IEnumerable<TModelDto>>();
                await Context.CreateWithoutSave(modelDataResp.Response, options?.ContextOptions);
                data.Add((modelDataResp, modelData));

                if ((i + 1) % MaxBulkLimit != 0 && i + 1 != modelDataList.Count) continue;
                // if this phase is complete then add to database, then clear from memory
                var response = await BulkCreateOperation(data, entities, options);
                // if operation has failed then return an error response
                if (!response.Success) return response.ToOtherResponse<IEnumerable<TModelDto>>();
                // clear list data
                data.Clear();
            }

            return new ServiceResponse<IEnumerable<TModelDto>>
            {
                Success = true,
                Response = MapperHelper.MapDto<TModelDto, TModel>(entities),
                StatusCode = StatusCodes.Status201Created
            };
        }


        public virtual async Task<IServiceResponse<TModelDto>> Create(TModelDto data)
        {
            var validationResponse = await ValidateData(data, Operations.Create);
            if (!validationResponse.Success) return validationResponse.ToOtherResponse<TModelDto>();

            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp.ToOtherResponse<TModelDto>();

            var createResponse = await _Create(createModelResp.Response);
            if (!createResponse.Success) return createResponse.ToOtherResponse<TModelDto>();

            var relationCreateResponse = await CreateRelationData(createResponse, data);
            if (relationCreateResponse.TriggerSave) await Context.Save();

            return MapperHelper.MapDto<TModelDto, TModel>(createResponse);
        }

        protected virtual IQueryable<TModelDto> SelectDto(IQueryable<TModel> set)
        {
            return set.ProjectTo<TModelDto>(Mapper.ConfigurationProvider);
        }


        public virtual async Task<IServiceResponse<TModel>> _Create(TModel data)
        {
            if (!await AuthorizeCreate(data)) return IServiceResponse<TModel>.Forbidden;

            var entity = await Context.Create(data);
            var triggerSave = await PostCreate(entity);
            if (triggerSave) await Context.Save();
            return new ServiceResponse<TModel>
            {
                Response = entity,
                Success = true,
                StatusCode = StatusCodes.Status201Created
            };
        }

        protected virtual async Task AfterBulkCreate(IEnumerable<TModel> models)
        {
            var createdModels = (await Context.Create(models)).ToList();
            if (await PostCreate(createdModels)) await Context.Save();
        }

        protected virtual async Task<IQueryable<TModel>> ApplyReadOptions(IQueryable<TModel> query,
            ServiceOptions<TModel> options = default)
        {
            if (query == null) return null;
            options ??= new ServiceOptions<TModel>
            {
                Filter = true,
                Sort = true,
                Includes = null
            };
            if (options.Includes != null)
                query = options.Includes.Aggregate(query, (current, include) => current.Include(include));

            return await ApplyReadOptions<TModel>(query, options);
        }

        protected virtual async Task<IQueryable<T>> ApplyReadOptions<T>(IQueryable<T> query,
            ServiceOptions<T> options = default)
        {
            if (query == null) return null;
            options ??= new ServiceOptions<T>
            {
                Filter = true,
                Sort = true
            };

            if (options.Filter) query = await ApplyFilter(query);
            if (options.Sort) query = Sort.SortByKey(query);

            return query;
        }

        protected virtual async Task<bool> AuthorizeCreate(TModel data)
        {
            return await AuthorizeOperation(data, Operations.Create);
        }

        protected virtual async Task<bool> AuthorizeUpdate(TModel data)
        {
            return await AuthorizeOperation(data, Operations.Update);
        }

        protected virtual async Task<bool> AuthorizeDelete(TModel data)
        {
            return await AuthorizeOperation(data, Operations.Delete);
        }


        protected virtual async Task<bool> AuthorizeRead(TModel data)
        {
            return await AuthorizeOperation(data, Operations.Read);
        }

        protected virtual async Task<bool> AuthorizeRead(TModelDto data)
        {
            return await AuthorizeOperation(MapperHelper.MapDto<TModel, TModelDto>(data), Operations.Read);
        }


        protected virtual async Task<bool> AuthorizeOperation(TModel data,
            OperationAuthorizationRequirement requirement)
        {
            var authResp =
                await Authorization.AuthorizeAsync(Accessor.HttpContext.User, data, requirement);
            return authResp.Succeeded;
        }

        protected virtual async Task<IServiceResponse> UpdateWithoutSave(TModel model)
        {
            if (!await AuthorizeUpdate(model))
            {
                Context.Detach(model);
                return IServiceResponse<TModel>.Forbidden;
            }

            Context.UpdateWithoutSave(model);

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        protected virtual async Task<PagingServiceResponse<TModelDto>> _readWithPaging(IQueryable<TModelDto> set)
        {
            return await PagingService.Apply(set);
        }

        protected virtual Task PreDelete(TModelKeyId id)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<IServiceResponse> PostUpdate(TModel model)
        {
            return Task.FromResult(new ServiceResponse
            {
                Success = true,
                TriggerSave = false
            } as IServiceResponse);
        }

        protected virtual async Task<bool> PostCreate(TModel model)
        {
            return await PostCreate(new[] {model});
        }

        protected virtual Task<bool> PostCreate(IEnumerable<TModel> models)
        {
            return Task.FromResult(false);
        }


        protected virtual Task<IQueryable<TModel>> ApplyFilter(IQueryable<TModel> set)
        {
            return Task.FromResult(set);
        }

        protected virtual Task<IQueryable<TProperty>> ApplyFilter<TProperty>(IQueryable<TProperty> set)
        {
            return Task.FromResult(set);
        }

        // protected virtual IEnumerable<TProperty> SelectManyNotMapped<T, TProperty>(IEnumerable<T> set, Expression body,
        //     ParameterExpression param)
        // {
        //     var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
        //     return set.SelectMany(expressionSelect.Compile());
        // }

        // protected virtual TProperty SelectOneNotMapped<T, TProperty>(IEnumerable<T> set, Expression body,
        //     ParameterExpression param)
        // {
        //     var expressionSelect = Expression.Lambda<Func<T, TProperty>>(body, param);
        //     return set.Select(expressionSelect.Compile()).FirstOrDefault();
        // }

        protected virtual async Task<IServiceResponse> BulkCreateOperation(
            IList<(IServiceResponse<TModel>, TModelDto)> data,
            IList<TModel> entities,
            ServiceOptions<TModel> options = default)
        {
            await Context.Save(options?.ContextOptions);
            var triggerSave = data.Count > 0;
            foreach (var (serviceResponse, modelData) in data)
            {
                serviceResponse.TriggerSave = true;
                var createRelationResponse = await CreateRelationData(serviceResponse, modelData);
                if (!createRelationResponse.Success)
                    return createRelationResponse;

                entities.Add(serviceResponse.Response);
            }

            if (await PostCreate(entities) || triggerSave) await Context.Save(options?.ContextOptions);

            PropertyHelper.EmptyRelationalData(entities);
            return new ServiceResponse
            {
                Success = true
            };
        }


        protected virtual async Task<IServiceResponse> BulkUpdateOperation(
            IList<(IServiceResponse<TModel>, TModelDto)> updateData)
        {
            await Context.Save();
            var triggerSave = updateData.Count > 0;
            foreach (var (serviceResponse, modelData) in updateData)
            {
                serviceResponse.TriggerSave = true;
                var updateRelationData = await UpdateRelationData(serviceResponse, modelData);
                if (!updateRelationData.Success) return updateRelationData;
                triggerSave = (await PostUpdate(serviceResponse.Response)).TriggerSave;
            }

            if (triggerSave) await Context.Save();

            return new ServiceResponse
            {
                Success = true
            };
        }

        protected virtual Task<IServiceResponse<TModel>> CreateRelationData(IServiceResponse<TModel> response,
            TModelDto data)
        {
            response.TriggerSave = false;
            return Task.FromResult(response);
        }

        protected virtual Task<IServiceResponse> UpdateRelationData(IServiceResponse<TModel> response,
            TModelDto data)
        {
            response.TriggerSave = false;
            return Task.FromResult(response as IServiceResponse);
        }

        protected virtual Task<IServiceResponse<TModel>> CreateModel(TModelDto data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = true,
                Response = Mapper.Map<TModel>(data),
                StatusCode = StatusCodes.Status200OK,
                TriggerSave = false
            } as IServiceResponse<TModel>);
        }


        protected virtual Task<IServiceResponse<TModel>> UpdateModel(TModel model, TModelDto data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = true,
                Response = Mapper.Map(data, model),
                StatusCode = StatusCodes.Status200OK,
                TriggerSave = false
            } as IServiceResponse<TModel>);
        }

        protected virtual async Task<IServiceResponse<TModel>> PrepareCreate(TModelDto data)
        {
            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp;

            return !await AuthorizeCreate(createModelResp.Response)
                ? IServiceResponse<TModel>.Forbidden
                : createModelResp;
        }

        protected virtual Task<IServiceResponse> ValidateData(IEnumerable<TModelDto> data,
            OperationAuthorizationRequirement operation)
        {
            return Task.FromResult(new ServiceResponse
            {
                Success = true
            } as IServiceResponse);
        }

        protected virtual async Task<IServiceResponse> ValidateData(TModelDto data,
            OperationAuthorizationRequirement operation)
        {
            return await ValidateData(new[] {data}, operation);
        }
    }
}