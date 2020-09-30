using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ApiTools.Context;
using ApiTools.Extensions;
using ApiTools.Helpers;
using ApiTools.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ApiTools.Services
{
    public partial class Service<TModel, TModelKeyId, TContext, TModelDto> where TContext : IContext<TModel, TModelKeyId> where TModel : ContextEntity<TModelKeyId> where TModelDto : class, IDtoEntity<TModelKeyId>, new() where TModelKeyId : new()
    {
        public async Task<IServiceResponse<object>> Read(string selectField, ServiceOptions<TModel> options = default,
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

            var query = await ApplyReadOptions(Context.Find(readOptions), options);

            if (query == null) return IServiceResponse<object>.Forbidden;

            var propertyInfo = PropertyHelper.PropertyInfo<TModel>(selectField, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel);
            if (propertyInfo == null) return IServiceResponse<object>.NotFound;

            if (Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)) ||
                Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)))
                return IServiceResponse<object>.NotFound;

            var model = await AccessPropertyFunc<TModel>(selectField, propertyInfo, query, options);

            if (model == null) return IServiceResponse<object>.NotFound;

            return new ServiceResponse<object>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }


        public virtual async Task<IServiceResponse<object>> Read(TModelKeyId id, string selectField,
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

            var query = await ApplyReadOptions(Context.Find(Context.FindById(id), readOptions), options);

            if (query == null) return IServiceResponse<object>.Forbidden;

            var propertyInfo = PropertyHelper.PropertyInfo<TModel>(selectField, options.EnablePropertyNesting,
                options.MaxPropertyNestingLevel);
            if (propertyInfo == null) return IServiceResponse<object>.NotFound;

            if (Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)) ||
                Attribute.IsDefined(propertyInfo, typeof(JsonIgnoreAttribute)))
                return IServiceResponse<object>.NotFound;

            var model = await AccessPropertyFunc<TModel>(selectField, propertyInfo, query, options);

            if (model == null) return IServiceResponse<object>.NotFound;

            return new ServiceResponse<object>
            {
                Success = true,
                Response = model,
                StatusCode = StatusCodes.Status200OK
            };
        }

        protected virtual async Task<object> SelectManyVirtual<T, TProperty, TPropertyKeyId>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options)
            where TProperty : ContextEntity<TPropertyKeyId> where TPropertyKeyId : new()
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var selectMany = query.SelectMany(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            if (selectQuery == null) return PagingServiceResponse<TProperty>.Empty();
            if (options?.SelectFieldEntityId != null)
                return await selectMany.FirstOrDefaultAsync(x => x.Id.ToString().Equals(options.SelectFieldEntityId));
            return await PagingService.Apply(selectQuery);
        }

        protected virtual async Task<object> SelectContextManyVirtual<T, TProperty, TPropertyKeyId>(
            IQueryable<T> query,
            Expression body,
            ParameterExpression param,
            ServiceOptions<TModel> options
        ) where TProperty : ContextEntity<TPropertyKeyId> where TPropertyKeyId : new()
        {
            var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
            var selectMany = query.SelectMany(expressionSelect);
            var selectQuery = SelectQuery(selectMany.AsQueryable());
            if (selectQuery == null) return Enumerable.Empty<TProperty>();
            if (options?.SelectFieldEntityId != null)
                return await selectMany.FirstOrDefaultAsync(x => x.Id.ToString().Equals(options.SelectFieldEntityId));
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
                if (options?.SelectFieldEntityId != null) return selectMany[int.Parse(options.SelectFieldEntityId)];
                return selectMany;
            }
            else
            {
                var expressionSelect = Expression.Lambda<Func<T, IEnumerable<TProperty>>>(body, param);
                var selectMany = await query.SelectMany(expressionSelect).ToListAsync();
                if (options?.SelectFieldEntityId != null) return selectMany[int.Parse(options.SelectFieldEntityId)];
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
            var baseType = PropertyHelper.BaseType(propertyInfo);
            var parameters = new List<object> {query};

            var genericTypes = new List<Type>
            {
                typeof(T),
                baseType
            };

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
                            if (propertyInfo.PropertyType.IsSubclassOf(typeof(DbEntity<,,>)))
                                methodType = GetType().GetMethod(nameof(SelectManyVirtual), flags);
                            else
                                methodType = GetType().GetMethod(nameof(SelectContextManyVirtual), flags);
                            genericTypes.Add(PropertyHelper.GetModelIdType(baseType));
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

                genericTypes.Add(PropertyHelper.GetModelIdType(baseType));

                parameters.Add(
                    PropertyHelper.AccessPropertyFromName(propertyName, options.EnablePropertyNesting,
                        options.MaxPropertyNestingLevel, options.EnableDashedProperty)
                );
            }

            parameters.Add(options);


            if (methodType == null) return null;
            if (baseType == null) return null;
            var method = methodType
                .MakeGenericMethod(genericTypes.ToArray());

            return await method.InvokeAsync(this, parameters.ToArray());
        }


        protected virtual async Task<object> SelectManyFromListVirtual<T, T2, TId>(
            IQueryable<T> queryable,
            IEnumerable<string> properties,
            ServiceOptions<TModel> options)
            where TId : new()
            where T2 : IContextEntity<TId>
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

            if (options?.SelectFieldEntityId != null)
                return await filtered.SingleAsync(x => x.Id.ToString().Equals(options.SelectFieldEntityId));

            return await PagingService.Apply(filtered);
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

        public async Task<IServiceResponse<object>> Read(
            TModelKeyId id,
            string selectField,
            string fieldId,
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
            if (!isPropertyAList) return IServiceResponse<object>.BadRequest;

            return await Read(id, selectField, options, readOptions);
        }
    }
}