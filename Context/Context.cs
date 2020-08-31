using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiTools.Extensions;
using ApiTools.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Context
{
    public interface IContext<TModel, in TModelKeyId>
    {
        Task CreateWithoutSave(TModel entities, ContextOptions options = default);
        Task CreateWithoutSave([NoEnumeration] IEnumerable<TModel> entities, ContextOptions options = default);
        Task<TModel> Create(TModel entity, ContextOptions options = default);

        Task<IEnumerable<TModel>> Create([NoEnumeration] IEnumerable<TModel> entities,
            ContextOptions options = default);


        Task<TModel> FindOne(TModelKeyId id, ContextOptions options = default);
        IQueryable<TModel> FindByIds(IEnumerable<TModelKeyId> ids, ContextOptions options = default);
        Task<TModel> FindOne(Expression<Func<TModel, bool>> expression, ContextOptions options = default);

        IQueryable<TModel> Find(Expression<Func<TModel, bool>> expression, ContextOptions options = default);

        [CanBeNull]
        IQueryable<TModel> Find(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default);

        Expression<Func<TModel, bool>> FindById(TModelKeyId id, ContextOptions options = default);

        IQueryable<TModel> Find(ContextOptions options = default);

        Task<TModel> Update(TModel entity, ContextOptions options = default);
        Task<IEnumerable<TModel>> Update(IEnumerable<TModel> entity, ContextOptions options = default);
        void UpdateWithoutSave(TModel entity, ContextOptions options = default);
        void UpdateWithoutSave([NoEnumeration] IEnumerable<TModel> entities, ContextOptions options = default);


        Task Delete(TModel entity, ContextOptions options = default);
        Task Delete([NoEnumeration] IEnumerable<TModel> entities, ContextOptions options = default);

        void DeleteWithoutSave([NoEnumeration] IEnumerable<TModel> entities, ContextOptions options = default);
        void DeleteWithoutSave(TModel entity, ContextOptions options = default);

        Task<bool> Exist(TModelKeyId id, ContextOptions options = default);
        Task<bool> Exist(Expression<Func<TModel, bool>> expression, ContextOptions options = default);
        Task<bool> Exist(IEnumerable<TModelKeyId> ids, ContextOptions options = default);
        Task<bool> Exist(IEnumerable<Expression<Func<TModel, bool>>> expressions, ContextOptions options = default);
        Task<bool> ExistAll(IEnumerable<Expression<Func<TModel, bool>>> expressions, ContextOptions options = default);

        IQueryable<TModel> ExistQuery(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default);

        IQueryable<TModel> Order(IQueryable<TModel> entities, ContextOptions options = default);
        void Detach(TModel entity, ContextOptions options = default);
        void Detach(IList<TModel> entity, ContextOptions options = default);

        Task Save(ContextOptions options = default);
    }

    public class Context<TModel, TModelKeyId> : IContext<TModel, TModelKeyId>
        where TModel : class, IContextEntity<TModelKeyId>
        where TModelKeyId : new()
    {
        private static readonly Type ModelKeyIdType = typeof(TModelKeyId);
        private readonly IDbContext _context;

        protected Context(IDbContext context)
        {
            _context = context;
        }

        protected virtual IQueryable<TModel> SetQuery { get; set; }

        /// <summary>
        ///     Adds entities to the set without calling the <c>Save</c> method.
        /// </summary>
        /// <param name="entities">Entities to be added to the set</param>
        /// <returns></returns>
        public virtual async Task CreateWithoutSave(IEnumerable<TModel> entities,
            ContextOptions options = default)
        {
            await _context.AddRangeAsync(entities);
        }

        /// <summary>
        ///     Adds single entity to the set without calling the <c>Save</c> method.
        /// </summary>
        /// <param name="entity">Entity to be added to the set</param>
        /// <returns></returns>
        public virtual async Task CreateWithoutSave(TModel entity, ContextOptions options = default)
        {
            await _context.AddAsync(entity);
        }

        /// <summary>
        ///     Adds the entities to the set and calls the <c>Save</c> method.
        /// </summary>
        /// <param name="entities">Entities to be added to the database</param>
        /// <returns>Entities after calling <c>Save</c> method</returns>
        public virtual async Task<IEnumerable<TModel>> Create(IEnumerable<TModel> entities,
            ContextOptions options)
        {
            //TODO: create proxy, instead of returning the actual model
            await CreateWithoutSave(entities, options);
            await Save(options);
            _context.Attach(entities);
            return entities;
        }

        /// <summary>
        ///     Adds single entity to the set and calls the <c>Save</c> method.
        /// </summary>
        /// <param name="entity">Single entity to be added to the database</param>
        /// <returns>The entity after calling the <c>Save</c> method</returns>
        public virtual async Task<TModel> Create(TModel entity, ContextOptions options = default)
        {
            options = InitiateOptions(options);
            var dbSet = GetDbSet<TModel>();
            var proxy = dbSet.CreateProxy();

            await _context.AddAsync(entity);
            await Save(options);
            Detach(entity);
            _context.Entry(proxy).CurrentValues.SetValues(entity);
            _context.Entry(proxy).State = EntityState.Added;
            _context.Attach(proxy);

            return proxy;
        }


        /// <summary>
        ///     Finds one entity that matches the same supplied id <code>WHERE id = @id</code>
        /// </summary>
        /// <param name="id">Entity's Id of type <c>TModelKeyId</c></param>
        /// <param name="options">Read options</param>
        /// <returns>Entity that matches the id, null if no match</returns>
        public virtual async Task<TModel> FindOne(TModelKeyId id, ContextOptions options = null)
        {
            return await FindOne(FindById(id), options);
        }

        /// <summary>
        ///     Finds one entity that matches the supplied expression.
        /// </summary>
        /// <example>
        ///     <code>
        /// TModel entity = await _context.FindOne(x => x.id == id);
        /// </code>
        /// </example>
        /// <param name="expression">an <c>Expression</c> that is translated to SQL then performed on the database.</param>
        /// <param name="options">Read options</param>
        /// <returns>Entity that matches the expression, <c>null</c> if no match</returns>
        public virtual async Task<TModel> FindOne(Expression<Func<TModel, bool>> expression,
            ContextOptions options = null)
        {
            var query = Find(expression, options);
            if (query == null) return default;
            var entity = await query.FirstOrDefaultAsync();
            if (options != null && options.Track == false && entity != null) Detach(entity);
            return entity;
        }

        /// <summary>
        ///     generates a find query for entities that have an id that exists in the supplied ids.
        /// </summary>
        /// <param name="ids">an <c>IEnumerable</c> of ids of type <c>TModelKeyId</c></param>
        /// <param name="options">Read options</param>
        /// <returns>a query for entities that have an id that exists in the supplied ids.</returns>
        public virtual IQueryable<TModel> FindByIds(IEnumerable<TModelKeyId> ids, ContextOptions options = default)
        {
            return Find(_findByIds(ids, options), options);
        }

        /// <summary>
        ///     generates a find query for entities that match the supplied expression.
        /// </summary>
        /// <param name="expression">an expression with the TModel which returns a logical expression (returns true)</param>
        /// <param name="options">Read options</param>
        /// <returns>a query for entities that match the expression</returns>
        public virtual IQueryable<TModel> Find(Expression<Func<TModel, bool>> expression,
            ContextOptions options = null)
        {
            return Find(new[] {expression}, options);
        }

        /// <summary>
        ///     generates a find query.
        /// </summary>
        /// <param name="options">Read options</param>
        /// <returns>find query</returns>
        public virtual IQueryable<TModel> Find(ContextOptions options = default)
        {
            return Find(Enumerable.Empty<Expression<Func<TModel, bool>>>(), options);
        }

        /// <summary>
        ///     generates a find query for entities that match the supplied expressions
        /// </summary>
        /// <param name="expressions">expressions with the TModel which returns a logical expression (returns true)</param>
        /// <param name="options">Read options</param>
        /// <returns>
        ///     query for entities that match the expressions (<c>AND</c> if options.UseAndInMultipleExpressions == true else
        ///     <c>OR</c>)
        /// </returns>
        public virtual IQueryable<TModel> Find(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = null)
        {
            return _find(expressions, options);
        }


        /// <summary>
        ///     updates entity
        /// </summary>
        /// <param name="entity">entity of type TModel</param>
        /// <returns>the updated entity</returns>
        public virtual async Task<TModel> Update(TModel entity, ContextOptions options = default)
        {
            options = InitiateOptions(options);
            // _context.entity(entity).State = EntityState.Modified;
            _context.Update(entity);
            await Save(options);
            return entity;
        }

        /// <summary>
        ///     updates entities
        /// </summary>
        /// <param name="entities">a collection of entities</param>
        /// <returns>updated entities</returns>
        public virtual async Task<IEnumerable<TModel>> Update([NoEnumeration] IEnumerable<TModel> entities,
            ContextOptions options = default)
        {
            options = InitiateOptions(options);
            _context.UpdateRange(entities);
            await Save(options);
            return entities;
        }

        /// <summary>
        ///     updates the entity without triggering the <c>Save</c> method (save the entity in memory)
        /// </summary>
        /// <param name="entity">the entity to be updated</param>
        public void UpdateWithoutSave(TModel entity, ContextOptions options = default)
        {
            _context.Update(entity);
            // _context.entity(entity).State = EntityState.Modified;
        }

        /// <summary>
        ///     updates the entities without triggering the <c>Save</c> method (save the entities in memory)
        /// </summary>
        /// <param name="entities">the entities to be updated</param>
        public void UpdateWithoutSave(IEnumerable<TModel> entities, ContextOptions options = default)
        {
            _context.UpdateRange(entities);
            // Set.UpdateRange(entities);
        }


        /// <summary>
        ///     deletes an entity
        /// </summary>
        /// <param name="entity">the entity to be deleted</param>
        public virtual async Task Delete(TModel entity, ContextOptions options = default)
        {
            options = InitiateOptions(options);
            _context.Remove(entity);
            await Save(options);
        }

        public virtual async Task Delete(IEnumerable<TModel> entities, ContextOptions options = default)
        {
            options = InitiateOptions(options);
            _context.RemoveRange(entities);
            await Save(options);
        }

        public virtual void DeleteWithoutSave(IEnumerable<TModel> entities, ContextOptions options = default)
        {
            _context.RemoveRange(entities);
        }

        public virtual void DeleteWithoutSave(TModel entity, ContextOptions options = default)
        {
            _context.Remove(entity);
        }


        public virtual void Detach(TModel entity, ContextOptions options = default)
        {
            if (entity != null) _context.Entry(entity).State = EntityState.Detached;
        }

        public virtual void Detach(IList<TModel> entities, ContextOptions options = default)
        {
            foreach (var entity in entities) _context.Entry(entity).State = EntityState.Detached;
        }

        public virtual async Task Save(ContextOptions options = default)
        {
            await _context.SaveChangesAsync();
        }

        public virtual Expression<Func<TModel, bool>> FindById(TModelKeyId id, ContextOptions options = default)
        {
            return _findById(id);
        }

        public virtual async Task<bool> Exist(TModelKeyId id, ContextOptions options = default)
        {
            return await Exist(_findById(id), options);
        }

        public virtual async Task<bool> Exist(Expression<Func<TModel, bool>> expression,
            ContextOptions options = default)
        {
            return await Exist(new[] {expression}, options);
        }

        public virtual async Task<bool> Exist(IEnumerable<TModelKeyId> ids,
            ContextOptions options = default)
        {
            if (options?.AllExist == true) return await Count(_findByIds(ids, options), options) == ids.LongCount();
            return await Exist(_findByIds(ids, options), options);
        }

        public virtual async Task<bool> Exist(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default)
        {
            return await AnyAsync(expressions, options);
        }

        public virtual async Task<bool> ExistAll(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default)
        {
            if (options != null) options.UseAndInMultipleExpressions = true;

            var expressionsList = expressions.ToList();
            var query = Find(expressionsList, options);
            if (query == null) return default;
            var result = await query.CountAsync();
            return result == expressionsList.Count;
        }

        public virtual IQueryable<TModel> ExistQuery(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default)
        {
            return Find(expressions, options);
        }


        public virtual IQueryable<TModel> Order(IQueryable<TModel> entities, ContextOptions options = default)
        {
            // if (PropertyHelper.BaseType(typeof(TModel)).IsSubclassOf(typeof(IDbEntity<>)))
            //     return entities.OrderBy(x => x.CreationTime);
            return entities;
        }

        protected virtual void Detach(IEnumerable<TModel> entities, ContextOptions options = default)
        {
            foreach (var contextEntity in entities) _context.Entry(contextEntity).State = EntityState.Detached;
        }

        protected virtual DbSet<TTModel> GetDbSet<TTModel>() where TTModel : class
        {
            return _context.Set<TTModel>();
        }

        protected virtual IQueryable<TModel> GetSetQuery()
        {
            return SetQuery;
        }

        private IQueryable<TTModel> GetSetQuery<TTModel>() where TTModel : class
        {
            return GetDbSet<TTModel>().AsQueryable();
        }

        private static Expression<Func<TModel, bool>> _findByIds(IEnumerable<TModelKeyId> ids,
            ContextOptions options)
        {
            return QueryableExtension.FindByIdsExpression<TModelKeyId, TModel>(ids);
            // var isPrimitiveType = ModelKeyIdType.IsPrimitive;
            // if (isPrimitiveType) return ;

            // var normalizedIds = listIds.Select(x => x.ToString()).ToList();
            // return x => normalizedIds.Contains(x.Id.ToString());
        }


        protected virtual async Task<bool> AnyAsync(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default)
        {
            var query = Find(expressions, options);
            if (query == null) return default;
            return await query.AnyAsync();
        }

        protected virtual async Task<long> Count(Expression<Func<TModel, bool>> expression,
            ContextOptions options = default)
        {
            var query = Find(expression, options);
            if (query == null) return default;
            return await query.LongCountAsync();
        }

        protected virtual IQueryable<TModel> _read(ContextOptions options = default)
        {
            var set = GetSetQuery();
            if (options != default && options.Order) set = Order(set);
            if (options != default && options.Query) set = GetQueryProvider(set);
            return set;
        }

        protected virtual IQueryable<TModel> _find(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextOptions options = default)
        {
            options = InitiateOptions(options);
            var set = _read(options);
            if (set == null) return null;
            if (expressions == null) return set;
            if (!expressions.Any()) return set;

            if (options.UseAndInMultipleExpressions)
                return expressions.Aggregate(set, (current, expression) => current.Where(expression));
            return set.Where(OrExpressions(expressions.ToList()));
        }

        protected virtual Expression<Func<TModel, bool>> OrExpressions(
            IList<Expression<Func<TModel, bool>>> expressions)
        {
            var param = Expression.Parameter(typeof(TModel), "x");

            Expression<Func<TModel, bool>> expression = null;
            if (expressions.Count == 1)
            {
                expression = expressions.First();
            }
            else if (expressions.Count > 1)
            {
                BinaryExpression binaryExpression = null;
                for (var i = expressions.Count - 1; i > 0; i--)
                {
                    var nextExpression = expressions[i - 1];
                    binaryExpression = Expression.Or(expressions[i], nextExpression);
                }

                if (binaryExpression != null)
                    expression = Expression.Lambda<Func<TModel, bool>>(binaryExpression, param);
            }

            return expression;
        }

        private static ContextOptions InitiateOptions(ContextOptions options = default)
        {
            options ??= new ContextOptions
            {
                Order = true,
                Query = true,
                Track = false,
                Upsert = false
            };
            return options;
        }


        private static Expression<Func<TModel, bool>> _findById(TModelKeyId id)
        {
            return x => x.Id.Equals(id);
        }

        protected virtual IQueryable<TModel> GetQueryProvider(IQueryable<TModel> set)
        {
            return set;
        }
    }
}