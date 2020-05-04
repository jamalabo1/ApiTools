﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiTools.Context;
using ApiTools.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Services
{
    public interface IService<TModel, in TModelKeyId>
        where TModel : DbEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        Task<ServiceResponse<TModel>> Read(TModelKeyId id,
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default);

        Task<ServiceResponse<IEnumerable<TModel>>> Read(IEnumerable<TModelKeyId> ids,
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default);

        Task<ServiceResponse<PagingServiceResponse<TModel>>> Read(
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default);

        Task<ServiceResponse> Delete(TModelKeyId id);
        Task<ServiceResponse> Delete(IEnumerable<TModelKeyId> ids);
    }

    public interface IService<TModel, TModelKeyId, TModelData>
        : IService<TModel, TModelKeyId>
        where TModel : DbEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        Task<ServiceResponse<TModel>> Create(TModelData data);
        Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModelData> data);

        Task<ServiceResponse> Update(TModelKeyId id, TModelData data);
        Task<ServiceResponse> Update(IReadOnlyList<BulkUpdateModel<TModelData, TModelKeyId>> bulkUpdateModels);
    }

    public abstract class
        Service<TModel, TModelKeyId, TContext> : IService<TModel, TModelKeyId>
        where TContext : IContext<TModel, TModelKeyId>
        where TModel : DbEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAuthorizationService _authorization;
        private readonly TContext _context;
        private readonly IPagingService _pagingService;
        private readonly ISort _sort;

        protected Service(TContext context, IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService, ISort sort)
        {
            _context = context;
            _authorization = authorization;
            _accessor = accessor;
            _pagingService = pagingService;
            _sort = sort;
        }

        protected Service(TContext context, IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService)
        {
            _context = context;
            _authorization = authorization;
            _accessor = accessor;
            _pagingService = pagingService;
        }


        public virtual async Task<ServiceResponse> Delete(TModelKeyId id)
        {
            var read = await Read(id, ServiceReadOptions<TModel>.DisableFilter);
            if (read.Response == null) return ServiceResponse<TModel>.NotFound;
            var authResp =
                await _authorization.AuthorizeAsync(_accessor.HttpContext.User, read.Response, Operations.Delete);
            if (!authResp.Succeeded) return ServiceResponse<TModel>.Forbidden;

            await PreDelete(id);
            await _context.Delete(read.Response);

            return new ServiceResponse
            {
                StatusCode = StatusCodes.Status204NoContent,
                Success = true
            };
        }


        public virtual async Task<ServiceResponse> Delete(IEnumerable<TModelKeyId> ids)
        {
            var enumerable = ids as TModelKeyId[] ?? ids.ToArray();
            var models = await Read(enumerable);
            if (models.Response == null) return models;

            foreach (var id in enumerable) await PreDelete(id);
            await _context.Delete(models.Response);

            return new ServiceResponse
            {
                StatusCode = StatusCodes.Status204NoContent,
                Success = true
            };
        }

        public virtual async Task<ServiceResponse<PagingServiceResponse<TModel>>> Read(
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default)
        {
            if (_sort == null)
                throw new MissingFieldException(
                    "Sort was not supplied from the constructor, this method cannot be called without it.");

            var query = await ApplyReadOptions(_context.Find(readOptions), options);
            var response = await _readWithPaging(query);

            return new ServiceResponse<PagingServiceResponse<TModel>>
            {
                Success = true,
                Response = response,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ServiceResponse<TModel>> Read(TModelKeyId id,
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default)
        {
            var query = await ApplyReadOptions(_context.Find(_context.FindById(id), readOptions), options);
            var model = await query.SingleOrDefaultAsync();

            if (model == null) return ServiceResponse<TModel>.NotFound;

            return new ServiceResponse<TModel>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ServiceResponse<IEnumerable<TModel>>> Read(IEnumerable<TModelKeyId> ids,
            ServiceReadOptions<TModel> options = default,
            ContextReadOptions readOptions = default)
        {
            var query = await ApplyReadOptions(_context.FindByIds(ids, readOptions), options);
            var models = await query.ToListAsync();

            foreach (var model in models)
            {
                var authResp = await _authorization.AuthorizeAsync(_accessor.HttpContext.User, model, Operations.Read);
                if (!authResp.Succeeded) return ServiceResponse<IEnumerable<TModel>>.Forbidden;
            }

            return new ServiceResponse<IEnumerable<TModel>>
            {
                Success = true,
                Response = models,
                StatusCode = StatusCodes.Status200OK
            };
        }


        protected virtual async Task<IQueryable<TModel>> ApplyReadOptions(IQueryable<TModel> query,
            ServiceReadOptions<TModel> options = default)
        {
            options ??= new ServiceReadOptions<TModel>
            {
                Filter = true,
                Sort = true,
                Includes = null
            };
            if (options.Includes != null)
                query = options.Includes.Aggregate(query, (current, include) => current.Include(include));

            if (options.Filter) query = await ApplyFilter(query);

            if (options.Sort) query = _sort.SortByKey(query);

            return query;
        }


        protected virtual async Task<ServiceResponse<TModel>> Create(TModel data)
        {
            var authResp =
                await _authorization.AuthorizeAsync(_accessor.HttpContext.User, data, Operations.Create);
            if (!authResp.Succeeded) return ServiceResponse<TModel>.Forbidden;

            var entity = await _context.Create(data);
            await PostCreate(entity);
            return new ServiceResponse<TModel>
            {
                Response = entity,
                Success = true,
                StatusCode = StatusCodes.Status201Created
            };
        }


        protected virtual async Task<ServiceResponse> Update(TModel data)
        {
            await UpdateWithoutSave(data);
            await _context.Save();
            var postUpdateResp = await PostUpdate(data);
            if (postUpdateResp.TriggerSave)
                await _context.Save();

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        protected virtual async Task<ServiceResponse> UpdateWithoutSave(TModel model)
        {
            var authResp = await _authorization.AuthorizeAsync(_accessor.HttpContext.User, model, Operations.Update);
            if (!authResp.Succeeded)
            {
                _context.Detach(model);
                return ServiceResponse<TModel>.Forbidden;
            }

            _context.UpdateWithoutSave(model);

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        protected virtual async Task<PagingServiceResponse<TModel>> _readWithPaging(IQueryable<TModel> set)
        {
            var entities = set;
            return await _pagingService.Apply(entities);
        }

        protected virtual Task PreDelete(TModelKeyId id)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<ServiceResponse> PostUpdate(TModel model)
        {
            return Task.FromResult(new ServiceResponse
            {
                Success = true,
                TriggerSave = false
            });
        }

        protected virtual Task PostCreate(TModel model)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<IQueryable<TModel>> ApplyFilter(IQueryable<TModel> set)
        {
            return Task.FromResult(set);
        }
    }

    public abstract class
        Service<TModel, TModelKeyId, TContext, TModelData> : Service<TModel, TModelKeyId, TContext>,
            IService<TModel, TModelKeyId, TModelData>
        where TContext : IContext<TModel, TModelKeyId>
        where TModel : DbEntity<TModelKeyId>
        where TModelData : class
        where TModelKeyId : new()
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAuthorizationService _authorization;
        private readonly TContext _context;
        private readonly IPagingService _pagingService;
        private readonly ISort _sort;

        protected Service(TContext context, IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService, ISort sort) : base(context, authorization, accessor, pagingService, sort)
        {
            _context = context;
            _authorization = authorization;
            _accessor = accessor;
            _pagingService = pagingService;
            _sort = sort;
        }

        protected Service(TContext context, IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService) : base(context, authorization, accessor, pagingService)
        {
            _context = context;
            _authorization = authorization;
            _accessor = accessor;
            _pagingService = pagingService;
        }

        public virtual async Task<ServiceResponse<TModel>> Create(TModelData data)
        {
            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp;

            var createResponse = await Create(createModelResp.Response);
            if (!createResponse.Success) return createResponse;

            var relationCreateResponse = await CreateRelationData(createResponse, data);
            if (relationCreateResponse.TriggerSave) await _context.Save();

            return relationCreateResponse;
        }

        public virtual async Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModelData> modelDataList)
        {
            var data = new List<(ServiceResponse<TModel>, TModelData)>();
            foreach (var modelData in modelDataList)
            {
                var modelDataResp = await PrepareCreate(modelData);
                if (!modelDataResp.Success)
                    return ServiceResponse<IEnumerable<TModel>>.FromOtherResponse(modelDataResp);
                await _context.CreateWithoutSave(modelDataResp.Response);
                data.Add((modelDataResp, modelData));
            }

            await _context.Save();

            var entities = new List<TModel>();
            foreach (var (serviceResponse, modelData) in data)
            {
                var createRelationResponse = await CreateRelationData(serviceResponse, modelData);
                if (!createRelationResponse.Success)
                    return ServiceResponse<IEnumerable<TModel>>.FromOtherResponse(createRelationResponse);
                entities.Add(serviceResponse.Response);
            }

            return new ServiceResponse<IEnumerable<TModel>>
            {
                Success = true,
                Response = entities,
                StatusCode = StatusCodes.Status201Created
            };
        }


        public virtual async Task<ServiceResponse> Update(TModelKeyId id, TModelData data)
        {
            var read = await Read(id, ServiceReadOptions<TModel>.DisableFilter);
            if (read.Response == null) return ServiceResponse<TModel>.NotFound;

            var updateModelResponse = await UpdateModel(read.Response, data);
            if (updateModelResponse.Response == null || updateModelResponse.Success == false)
                return updateModelResponse;

            var updateResponse = await Update(updateModelResponse.Response);
            if (updateResponse.Success == false) return updateResponse;
            updateModelResponse.StatusCode = updateResponse.StatusCode;
            var updateRelationResponse = await UpdateRelationData(updateModelResponse, data);
            if (updateRelationResponse.TriggerSave) await _context.Save();
            return updateRelationResponse;
        }

        public virtual async Task<ServiceResponse> Update(
            IReadOnlyList<BulkUpdateModel<TModelData, TModelKeyId>> bulkUpdateModels)
        {
            var modelsReadResponse = await Read(bulkUpdateModels.Select(x => x.Id));
            if (!modelsReadResponse.Success) return modelsReadResponse;
            var models = modelsReadResponse.Response;

            var updateData = new List<(ServiceResponse<TModel>, TModelData)>();
            foreach (var model in models)
            {
                var modelData = bulkUpdateModels.FirstOrDefault(x => x.Id.Equals(model.Id))?.Entity;
                if (modelData == null) return ServiceResponse<IEnumerable<TModel>>.BadRequest;
                var updateModelResponse = await UpdateModel(model, modelData);
                if (!updateModelResponse.Success) return updateModelResponse;
                var updateResponse = await UpdateWithoutSave(updateModelResponse.Response);
                if (!updateResponse.Success) return updateResponse;
                updateData.Add((updateModelResponse, modelData));
            }

            await _context.Save();

            var triggerSave = true;
            foreach (var (serviceResponse, modelData) in updateData)
            {
                var updateRelationData = await UpdateRelationData(serviceResponse, modelData);
                if (!updateRelationData.Success) return updateRelationData;
                triggerSave = (await PostUpdate(serviceResponse.Response)).TriggerSave;
            }

            if (triggerSave) await _context.Save();

            return new ServiceResponse
            {
                Success = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        protected virtual Task<ServiceResponse<TModel>> CreateRelationData(ServiceResponse<TModel> response,
            TModelData data)
        {
            return Task.FromResult(response);
        }

        protected virtual Task<ServiceResponse> UpdateRelationData(ServiceResponse<TModel> response,
            TModelData data)
        {
            return Task.FromResult((ServiceResponse) response);
        }

        protected virtual Task<ServiceResponse<TModel>> CreateModel(TModelData data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = false,
                Response = default,
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }

        protected virtual Task<ServiceResponse<TModel>> UpdateModel(TModel model, TModelData data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = false,
                Response = default,
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }

        protected virtual async Task<ServiceResponse<TModel>> PrepareCreate(TModelData data)
        {
            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp;
            var authResp =
                await _authorization.AuthorizeAsync(_accessor.HttpContext.User, createModelResp.Response,
                    Operations.Create);
            return !authResp.Succeeded ? ServiceResponse<TModel>.Forbidden : createModelResp;
        }
    }
}