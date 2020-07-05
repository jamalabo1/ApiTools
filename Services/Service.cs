using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ApiTools.Context;
using ApiTools.Extensions;
using ApiTools.Helpers;
using ApiTools.Models;
using ApiTools.Provider;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Services
{
    public interface IService<TModel, in TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        Task<ServiceResponse<TModel>> Create(TModel model);
        Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModel> models);

        Task<ServiceResponse<TModel>> Read(TModelKeyId id,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse<IEnumerable<TModel>>> Read(IEnumerable<TModelKeyId> ids,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse<PagingServiceResponse<TModel>>> Read(
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse> Delete(TModelKeyId id);
        Task<ServiceResponse> Delete(IEnumerable<TModelKeyId> ids);

        Task<ServiceResponse<object>> Read(TModelKeyId id,
            string selectField,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse<object>> Read(string selectField,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse<object>> Read(TModelKeyId id, string selectField,
            long fieldId,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default);

        Task<ServiceResponse> Update(TModel model);
    }

    public interface IService<TModel, TModelKeyId, TModelData>
        : IService<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        Task<ServiceResponse<TModel>> Create(TModelData data);

        Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModelData> dataModels,
            ServiceOptions<TModel> options = default);

        Task<ServiceResponse> Update(TModelKeyId id, TModelData data);
        Task<ServiceResponse> Update(IReadOnlyList<BulkUpdateModel<TModelData, TModelKeyId>> bulkUpdateModels);
    }

    public abstract class
        Service<TModel, TModelKeyId, TContext> : IService<TModel, TModelKeyId>
        where TContext : IContext<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
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

        protected virtual int MaxBulkLimit { get; set; } = 1000;


        public virtual async Task<ServiceResponse> Delete(TModelKeyId id)
        {
            var read = await Read(id, ServiceOptions<TModel>.DisableFilter);
            if (read.Response == null) return ServiceResponse<TModel>.NotFound;

            if (!await AuthorizeDelete(read.Response)) return ServiceResponse<TModel>.Forbidden;

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
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            if (_sort == null)
                throw new MissingMemberException(
                    "Sort was not supplied from the constructor, this method cannot be called without it.");

            var query = await ApplyReadOptions(_context.Find(readOptions), options);

            if (query == null) return ServiceResponse<PagingServiceResponse<TModel>>.Forbidden;

            var response = await _readWithPaging(query);

            return new ServiceResponse<PagingServiceResponse<TModel>>
            {
                Success = true,
                Response = response,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public virtual async Task<ServiceResponse<TModel>> Read(TModelKeyId id,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            var query = await ApplyReadOptions(_context.Find(_context.FindById(id), readOptions), options);

            if (query == null) return ServiceResponse<TModel>.Forbidden;

            var model = await query.SingleOrDefaultAsync();

            if (!await AuthorizeRead(model)) return ServiceResponse<TModel>.Forbidden;

            if (model == null) return ServiceResponse<TModel>.NotFound;

            return new ServiceResponse<TModel>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public virtual async Task<ServiceResponse<IEnumerable<TModel>>> Read(IEnumerable<TModelKeyId> ids,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            var query = await ApplyReadOptions(_context.FindByIds(ids, readOptions), options);
            if (query == null) return ServiceResponse<IEnumerable<TModel>>.Forbidden;

            var models = await query.ToListAsync();

            foreach (var model in models)
                if (!await AuthorizeRead(model))
                    return ServiceResponse<IEnumerable<TModel>>.Forbidden;

            return new ServiceResponse<IEnumerable<TModel>>
            {
                Success = true,
                Response = models,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ServiceResponse<object>> Read(string selectField, ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            options ??= new ServiceOptions<TModel>
            {
                Filter = true,
                Includes = ArraySegment<Expression<Func<TModel, dynamic>>>.Empty,
                Sort = false,
                EnableDashedProperty = true,
                EnablePropertyNesting = true,
                SelectFieldMany = true
            };

            var query = await ApplyReadOptions(_context.Find(readOptions), options);

            if (query == null) return ServiceResponse<object>.Forbidden;

            var propertyInfo = PropertyHelper.PropertyInfo<TModel>(selectField, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel);
            if (propertyInfo == null) return ServiceResponse<object>.NotFound;

            if (Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)) ||
                Attribute.IsDefined(propertyInfo, typeof(Newtonsoft.Json.JsonIgnoreAttribute)))
                return ServiceResponse<object>.NotFound;

            var model = await AccessPropertyFunc<TModel>(selectField, propertyInfo, query, options);

            if (model == null) return ServiceResponse<object>.NotFound;

            return new ServiceResponse<object>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ServiceResponse<object>> Read(
            TModelKeyId id,
            string selectField,
            long fieldId,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            options ??= new ServiceOptions<TModel>
            {
                Filter = true,
                Includes = ArraySegment<Expression<Func<TModel, dynamic>>>.Empty,
                Sort = false,
                EnableDashedProperty = true,
                EnablePropertyNesting = true,
                SelectFieldEntityId = fieldId
            };

            var propertyInfo = PropertyHelper.PropertyInfo<TModel>(selectField, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel);
            var isPropertyAList = PropertyHelper.IsPropertyAList(propertyInfo);
            if (!isPropertyAList) return ServiceResponse<object>.BadRequest;

            return await Read(id, selectField, options, readOptions);
        }


        public virtual async Task<ServiceResponse<object>> Read(TModelKeyId id, string selectField,
            ServiceOptions<TModel> options = default,
            ContextOptions readOptions = default)
        {
            options ??= new ServiceOptions<TModel>
            {
                Filter = false,
                Includes = ArraySegment<Expression<Func<TModel, dynamic>>>.Empty,
                Sort = false,
                EnableDashedProperty = true,
                EnablePropertyNesting = true
            };

            var query = await ApplyReadOptions(_context.Find(_context.FindById(id), readOptions), options);

            if (query == null) return ServiceResponse<object>.Forbidden;

            var propertyInfo = PropertyHelper.PropertyInfo<TModel>(selectField, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel);
            if (propertyInfo == null) return ServiceResponse<object>.NotFound;

            if (Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)) ||
                Attribute.IsDefined(propertyInfo, typeof(Newtonsoft.Json.JsonIgnoreAttribute)))
                return ServiceResponse<object>.NotFound;

            var model = await AccessPropertyFunc<TModel>(selectField, propertyInfo, query, options);

            if (model == null) return ServiceResponse<object>.NotFound;

            return new ServiceResponse<object>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }


        public virtual async Task<ServiceResponse<TModel>> Create(TModel data)
        {
            if (!await AuthorizeCreate(data)) return ServiceResponse<TModel>.Forbidden;

            var entity = await _context.Create(data);
            var triggerSave = await PostCreate(entity);
            if (triggerSave) await _context.Save();
            return new ServiceResponse<TModel>
            {
                Response = entity,
                Success = true,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModel> models)
        {
            var listModels = models.ToList();

            for (var i = 0; i < listModels.Count; i++)
            {
                var model = listModels[i];
                if (!await AuthorizeCreate(model))
                    return ServiceResponse<IEnumerable<TModel>>.Forbidden;

                if ((i + 1) % MaxBulkLimit != 0 && i + 1 != listModels.Count) continue;
                await AfterBulkCreate(listModels.Skip(MaxBulkLimit * ((i + 1) / MaxBulkLimit)).Take(i));
            }


            return new ServiceResponse<IEnumerable<TModel>>
            {
                Response = listModels,
                Success = true,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public virtual async Task<ServiceResponse> Update(TModel data)
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

        protected virtual async Task AfterBulkCreate(IEnumerable<TModel> models)
        {
            var createdModels = (await _context.Create(models)).ToList();
            if (await PostCreate(createdModels)) await _context.Save();
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

            if (options.Filter) query = await ApplyFilter(query);
            if (options.Sort) query = _sort.SortByKey(query);

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


        protected virtual async Task<bool> AuthorizeOperation(TModel data,
            OperationAuthorizationRequirement requirement)
        {
            var authResp =
                await _authorization.AuthorizeAsync(_accessor.HttpContext.User, data, requirement);
            return authResp.Succeeded;
        }

        protected virtual async Task<ServiceResponse> UpdateWithoutSave(TModel model)
        {
            if (!await AuthorizeUpdate(model))
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

        protected virtual async Task<PagingServiceResponse<TProperty>> SelectManyVirtual<T, TProperty>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options)
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var selectMany = query.SelectMany(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            if (selectQuery == null) return PagingServiceResponse<TProperty>.Empty();
            return await _pagingService.Apply(selectQuery);
        }

        protected virtual async Task<object> SelectContextManyVirtual<T, TProperty>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options
        ) where TProperty : ContextEntity<long>
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var selectMany = query.SelectMany(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            if (selectQuery == null) return Enumerable.Empty<TProperty>();
            if (options?.SelectFieldEntityId != null)
                return await selectMany.FirstOrDefaultAsync(x => x.Id == options.SelectFieldEntityId);
            return await selectQuery.ToListAsync();
        }

        protected virtual async Task<dynamic> SelectMany<T, TProperty>(IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            bool isArray,
            ServiceOptions<TModel> options)
        {
            if (isArray)
            {
                var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
                var selectMany = await query.SelectMany(expressionSelect).ToListAsync();
                if (options?.SelectFieldEntityId != null) return selectMany[(int) options.SelectFieldEntityId];
                return selectMany;
            }
            else
            {
                var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
                var selectMany = await query.SelectMany(expressionSelect).ToListAsync();
                if (options?.SelectFieldEntityId != null) return selectMany[(int) options.SelectFieldEntityId];
                return selectMany;
            }
        }

        protected virtual async Task<dynamic> SelectOneVirtual<T, TProperty>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options
        )
        {
            var expressionSelect = Expression.Lambda<Func<T, TProperty>>(body, param);
            var selectMany = query.Select(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            if (selectQuery == null) return default;
            if (options.SelectFieldMany) return await selectMany.ToListAsync();
            return await selectQuery.SingleOrDefaultAsync();
        }

        protected virtual async Task<dynamic> SelectOne<T, TProperty>(IQueryable<T> query, Expression body,
            ParameterExpression param, ServiceOptions<TModel> options)
        {
            var expressionSelect = Expression.Lambda<Func<T, TProperty>>(body, param);
            var selectMany = query.Select(expressionSelect);
            if (options.SelectFieldMany) return await selectMany.ToListAsync();
            return await selectMany.SingleOrDefaultAsync();
        }

        protected virtual IQueryable<TProperty> SelectQuery<TProperty>(IQueryable<TProperty> query)
        {
            return query;
        }


        protected virtual async Task<object> AccessPropertyFunc<T>(string propertyName, PropertyInfo propertyInfo,
            IQueryable<TModel> query, ServiceOptions<TModel> options)
        {
            var parameters = new List<object> {query};
            MethodInfo methodType;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            if (!PropertyHelper.IsAccessingAList<T>(propertyName, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel, options.EnableDashedProperty))
            {
                var (body, param) = PropertyHelper.PropertyFunc<T>(propertyName, options.EnablePropertyNesting,
                    options.MaxPropertyNestingLevel, options.EnableDashedProperty);
                parameters.AddRange(new[] {body, param});

                if (Attribute.IsDefined(propertyInfo, typeof(NotMappedAttribute)))
                {
                    methodType = GetType().GetMethod(nameof(SelectOneNotMapped), flags);
                }
                else
                {
                    if (propertyInfo.GetAccessors()[0].IsVirtual)
                    {
                        if (PropertyHelper.IsPropertyAList(propertyInfo))
                        {
                            if (propertyInfo.PropertyType.IsSubclassOf(typeof(DbEntity<>)))
                                methodType = GetType().GetMethod(nameof(SelectManyVirtual), flags);
                            else
                                methodType = GetType().GetMethod(nameof(SelectContextManyVirtual), flags);
                        }
                        else
                        {
                            methodType = GetType().GetMethod(nameof(SelectOneVirtual), flags);
                        }
                    }
                    else
                    {
                        if (propertyInfo.PropertyType.IsArray)
                        {
                            methodType = GetType()
                                .GetMethod(nameof(SelectMany), flags);
                            parameters.Add(true);
                        }
                        else if (PropertyHelper.IsPropertyAList(propertyInfo))
                        {
                            methodType = GetType()
                                .GetMethod(nameof(SelectMany), flags);
                            parameters.Add(false);
                        }
                        else
                        {
                            methodType = GetType().GetMethod(nameof(SelectOne), flags);
                        }
                    }
                }
            }
            else
            {
                if (Attribute.IsDefined(propertyInfo, typeof(NotMappedAttribute)))
                    methodType = GetType().GetMethod(nameof(SelectManyNotMapped), flags);
                else
                    methodType = GetType().GetMethod(nameof(SelectManyFromListVirtual), flags);

                parameters.Add(
                    PropertyHelper.AccessPropertyFromName(propertyName, options.EnablePropertyNesting,
                        options.MaxPropertyNestingLevel, options.EnableDashedProperty)
                );
            }

            parameters.Add(options);

            var baseType = PropertyHelper.BaseType(propertyInfo);

            if (methodType == null) return null;
            if (baseType == null) return null;
            var method = methodType
                .MakeGenericMethod(typeof(T),
                    baseType
                );

            return await method.InvokeAsync(this, parameters.ToArray());
        }


        protected virtual async Task<PagingServiceResponse<T2>> SelectManyFromListVirtual<T, T2>(
            IQueryable<T> queryable,
            IEnumerable<string> properties,
            ServiceOptions<TModel> options)
        {
            dynamic queryResult = queryable;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var currentType = typeof(T);
            foreach (var propertyName in properties)
            {
                if (queryResult == null) break;

                var (body, param) = PropertyHelper.PropertyFunc(currentType, propertyName,
                    options.EnablePropertyNesting, options.MaxPropertyNestingLevel, options.EnableDashedProperty);

                var propInfo = PropertyHelper.PropertyInfo(currentType, propertyName, options.EnablePropertyNesting,
                    options.MaxPropertyNestingLevel, options.EnableDashedProperty);
                var baseType = PropertyHelper.BaseType(propInfo);


                var method = GetType()
                    .GetMethod(
                        PropertyHelper.IsPropertyAList(propInfo)
                            ? nameof(SelectManyQueryFromListVirtual)
                            : nameof(SelectQueryFromListVirtual), flags)
                    ?.MakeGenericMethod(currentType,
                        baseType);


                var parameters = new List<object> {queryResult, body, param, options};

                var result = method?.Invoke(this, parameters.ToArray());
                queryResult = result;
                currentType = baseType;
            }

            if (queryResult == null) return PagingServiceResponse<T2>.Empty();
            var filtered = await ApplyFilter((IQueryable<T2>) queryResult);
            return await _pagingService.Apply(filtered);
        }

        protected virtual IQueryable<TProperty> SelectManyQueryFromListVirtual<T, TProperty>(IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options)
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var selectMany = query.SelectMany(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            return selectQuery;
        }

        protected virtual IQueryable<TProperty> SelectQueryFromListVirtual<T, TProperty>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options)
        {
            var expressionSelect = Expression.Lambda<Func<T, TProperty>>(body, param);
            var selectMany = query.Select(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            return selectQuery;
        }

        protected virtual async Task<IEnumerable<TProperty>> SelectManyNotMapped<T, TProperty>(
            IQueryable<T> set,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options
        )
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var data = await set.ToListAsync();
            return data.SelectMany(expressionSelect.Compile());
        }

        protected virtual async Task<dynamic> SelectOneNotMapped<T, TProperty>(
            IQueryable<T> set,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options
        )
        {
            var expressionSelect = Expression.Lambda<Func<T, TProperty>>(body, param);
            var data = await set.ToListAsync();
            var result = data.Select(expressionSelect.Compile());
            if (options.SelectFieldMany) return result;
            return result.FirstOrDefault();
        }
    }

    public abstract class
        Service<TModel, TModelKeyId, TContext, TModelData> : Service<TModel, TModelKeyId, TContext>,
            IService<TModel, TModelKeyId, TModelData>
        where TContext : IContext<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelData : class
        where TModelKeyId : new()
    {
        private readonly TContext _context;
        private readonly IMapper _mapper;

        protected Service(TContext context, IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService, ISort sort, IMapper mapper) : base(context, authorization, accessor,
            pagingService, sort)
        {
            _context = context;
            _mapper = mapper;
        }

        protected Service(TContext context, IAuthorizationService authorization,
            IHttpContextAccessor accessor,
            IPagingService pagingService, IMapper mapper) : base(context, authorization, accessor, pagingService)
        {
            _context = context;
            _mapper = mapper;
        }

        public virtual async Task<ServiceResponse<TModel>> Create(TModelData data)
        {
            var validationResponse = await ValidateData(data);
            if (!validationResponse.Success) return ServiceResponse<TModel>.FromOtherResponse(validationResponse);

            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp;

            var createResponse = await Create(createModelResp.Response);
            if (!createResponse.Success) return createResponse;

            var relationCreateResponse = await CreateRelationData(createResponse, data);
            if (relationCreateResponse.TriggerSave) await _context.Save();

            PropertyHelper.EmptyRelationalData(new[] {relationCreateResponse.Response});

            return relationCreateResponse;
        }

        public virtual async Task<ServiceResponse> Update(TModelKeyId id, TModelData data)
        {
            var read = await Read(id, ServiceOptions<TModel>.DisableFilter);
            if (read.Response == null) return ServiceResponse<TModel>.NotFound;

            if (!await AuthorizeUpdate(read.Response)) return ServiceResponse<TModel>.Forbidden;

            var validationResponse = await ValidateData(data);
            if (!validationResponse.Success) return ServiceResponse<TModel>.FromOtherResponse(validationResponse);

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
            var models = modelsReadResponse.Response.ToList();

            var validationResponse = await ValidateData(bulkUpdateModels.Select(x => x.Entity));
            if (!validationResponse.Success) return ServiceResponse<TModel>.FromOtherResponse(validationResponse);

            var updateData = new List<(ServiceResponse<TModel>, TModelData)>();
            for (var i = 0; i < models.Count; i++)
            {
                var model = models[i];
                var modelData = bulkUpdateModels.FirstOrDefault(x => x.Id.Equals(model.Id))?.Entity;
                if (modelData == null) return ServiceResponse<IEnumerable<TModel>>.BadRequest;
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

        public virtual async Task<ServiceResponse<IEnumerable<TModel>>> Create(IEnumerable<TModelData> dataModels,
            ServiceOptions<TModel> options)
        {
            var modelDataList = dataModels.ToList();

            var validationResponse = await ValidateData(modelDataList);
            if (!validationResponse.Success)
                return ServiceResponse<IEnumerable<TModel>>.FromOtherResponse(validationResponse);

            var entities = new List<TModel>();
            var data = new List<(ServiceResponse<TModel>, TModelData)>();
            // foreach (var modelData in modelDataList)
            for (var i = 0; i < modelDataList.Count; i++)
            {
                var modelData = modelDataList[i];
                var modelDataResp = await PrepareCreate(modelData);
                if (!modelDataResp.Success)
                    return ServiceResponse<IEnumerable<TModel>>.FromOtherResponse(modelDataResp);
                await _context.CreateWithoutSave(modelDataResp.Response, options?.ContextOptions);
                data.Add((modelDataResp, modelData));

                if ((i + 1) % MaxBulkLimit != 0 && i + 1 != modelDataList.Count) continue;
                // if this phase is complete then add to database, then clear from memory
                var response = await BulkCreateOperation(data, entities, options);
                // if operation has failed then return an error response
                if (!response.Success) return ServiceResponse<IEnumerable<TModel>>.FromOtherResponse(response);
                // clear list data
                data.Clear();
            }

            return new ServiceResponse<IEnumerable<TModel>>
            {
                Success = true,
                Response = entities,
                StatusCode = StatusCodes.Status201Created
            };
        }

        protected virtual async Task<ServiceResponse> BulkCreateOperation(
            IList<(ServiceResponse<TModel>, TModelData)> data,
            IList<TModel> entities,
            ServiceOptions<TModel> options = default)
        {
            await _context.Save(options?.ContextOptions);
            var triggerSave = data.Count > 0;
            foreach (var (serviceResponse, modelData) in data)
            {
                serviceResponse.TriggerSave = true;
                var createRelationResponse = await CreateRelationData(serviceResponse, modelData);
                if (!createRelationResponse.Success)
                    return createRelationResponse;

                entities.Add(serviceResponse.Response);
            }

            if (await PostCreate(entities) || triggerSave) await _context.Save(options?.ContextOptions);

            PropertyHelper.EmptyRelationalData(entities);
            return new ServiceResponse
            {
                Success = true
            };
        }


        protected virtual async Task<ServiceResponse> BulkUpdateOperation(
            IList<(ServiceResponse<TModel>, TModelData)> updateData)
        {
            await _context.Save();
            var triggerSave = updateData.Count > 0;
            foreach (var (serviceResponse, modelData) in updateData)
            {
                serviceResponse.TriggerSave = true;
                var updateRelationData = await UpdateRelationData(serviceResponse, modelData);
                if (!updateRelationData.Success) return updateRelationData;
                triggerSave = (await PostUpdate(serviceResponse.Response)).TriggerSave;
            }

            if (triggerSave) await _context.Save();

            return new ServiceResponse
            {
                Success = true
            };
        }

        protected virtual Task<ServiceResponse<TModel>> CreateRelationData(ServiceResponse<TModel> response,
            TModelData data)
        {
            response.TriggerSave = false;
            return Task.FromResult(response);
        }

        protected virtual Task<ServiceResponse> UpdateRelationData(ServiceResponse<TModel> response,
            TModelData data)
        {
            response.TriggerSave = false;
            return Task.FromResult((ServiceResponse) response);
        }

        protected virtual Task<ServiceResponse<TModel>> CreateModel(TModelData data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = true,
                Response = _mapper.Map<TModel>(data),
                StatusCode = StatusCodes.Status200OK,
                TriggerSave = false
            });
        }


        protected virtual Task<ServiceResponse<TModel>> UpdateModel(TModel model, TModelData data)
        {
            return Task.FromResult(new ServiceResponse<TModel>
            {
                Success = true,
                Response = _mapper.Map(data, model),
                StatusCode = StatusCodes.Status200OK,
                TriggerSave = false
            });
        }

        protected virtual async Task<ServiceResponse<TModel>> PrepareCreate(TModelData data)
        {
            var createModelResp = await CreateModel(data);
            if (!createModelResp.Success) return createModelResp;

            return !await AuthorizeCreate(createModelResp.Response)
                ? ServiceResponse<TModel>.Forbidden
                : createModelResp;
        }

        protected virtual Task<ServiceResponse> ValidateData(IEnumerable<TModelData> data)
        {
            return Task.FromResult(new ServiceResponse
            {
                Success = true
            });
        }

        protected virtual async Task<ServiceResponse> ValidateData(TModelData data)
        {
            return await ValidateData(new[] {data});
        }
    }

    public class
        InternalService<TModel, TModelKeyId, TResourceProvider> : Service<TModel, TModelKeyId,
            IContext<TModel, TModelKeyId>>
        where TModel : ContextEntity<TModelKeyId>
        where TModelKeyId : new()
        where TResourceProvider : IResourceQueryProvider
    {
        private readonly TResourceProvider _resourceQueryProvider;

        public InternalService(IContext<TModel, TModelKeyId> context, IAuthorizationService authorization,
            IHttpContextAccessor accessor, IPagingService pagingService, ISort sort,
            TResourceProvider resourceQueryProvider) : base(context, authorization,
            accessor, pagingService, sort)
        {
            _resourceQueryProvider = resourceQueryProvider;
        }

        public InternalService(IContext<TModel, TModelKeyId> context, IAuthorizationService authorization,
            IHttpContextAccessor accessor, IPagingService pagingService, TResourceProvider resourceQueryProvider) :
            base(context, authorization, accessor,
                pagingService)
        {
            _resourceQueryProvider = resourceQueryProvider;
        }

        protected override IQueryable<TProperty> SelectQuery<TProperty>(IQueryable<TProperty> query)
        {
            return _resourceQueryProvider.GetQuery((dynamic) query);
        }
    }

    public class
        InternalService<TModel, TModelKeyId, TModelData, TResourceProvider, TFilter> : Service<TModel, TModelKeyId,
            IContext<TModel, TModelKeyId>,
            TModelData>
        where TModel : ContextEntity<TModelKeyId>
        where TModelData : class
        where TModelKeyId : new()
        where TResourceProvider : IResourceQueryProvider
        where TFilter : IFilter
    {
        private readonly TFilter _filter;
        private readonly TResourceProvider _resourceQueryProvider;

        public InternalService(IContext<TModel, TModelKeyId> context, IAuthorizationService authorization,
            IHttpContextAccessor accessor, IPagingService pagingService, ISort sort, IMapper mapper,
            TResourceProvider resourceQueryProvider, TFilter filter) : base(context,
            authorization, accessor, pagingService, sort, mapper)
        {
            _resourceQueryProvider = resourceQueryProvider;
            _filter = filter;
        }

        public InternalService(IContext<TModel, TModelKeyId> context, IAuthorizationService authorization,
            IHttpContextAccessor accessor, IPagingService pagingService, IMapper mapper,
            TResourceProvider resourceQueryProvider, TFilter filter) : base(context, authorization,
            accessor, pagingService, mapper)
        {
            _resourceQueryProvider = resourceQueryProvider;
            _filter = filter;
        }

        protected override IQueryable<TProperty> SelectQuery<TProperty>(IQueryable<TProperty> query)
        {
            return _resourceQueryProvider.GetQuery((dynamic) query);
        }

        protected override Task<IQueryable<TProperty>> ApplyFilter<TProperty>(IQueryable<TProperty> set)
        {
            return Task.FromResult((IQueryable<TProperty>) _filter.ApplyFilter((dynamic) set));
        }

        protected override Task<IQueryable<TModel>> ApplyFilter(IQueryable<TModel> set)
        {
            return Task.FromResult(_filter.ApplyFilter(set));
        }
    }
}