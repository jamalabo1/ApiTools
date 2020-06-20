using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiTools.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Context
{
    public interface IContext<TModel, in TModelKeyId>
    {
        Task CreateWithoutSave(TModel entities);
        Task CreateWithoutSave([NoEnumeration] IEnumerable<TModel> entities);
        Task<TModel> Create(TModel entity);
        Task<IEnumerable<TModel>> Create([NoEnumeration] IEnumerable<TModel> entities);


        Task<TModel> FindOne(TModelKeyId id, ContextReadOptions options = default);
        IQueryable<TModel> FindByIds(IEnumerable<TModelKeyId> ids, ContextReadOptions options = default);
        Task<TModel> FindOne(Expression<Func<TModel, bool>> expression, ContextReadOptions options = default);

        IQueryable<TModel> Find(Expression<Func<TModel, bool>> expression, ContextReadOptions options = default);

        IQueryable<TModel> Find(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextReadOptions options = default);

        Expression<Func<TModel, bool>> FindById(TModelKeyId id);

        IQueryable<TModel> Find(ContextReadOptions options = default);


        Task<TModel> Update(TModel entity);
        Task<IEnumerable<TModel>> Update(IEnumerable<TModel> entity);
        void UpdateWithoutSave(TModel entity);
        void UpdateWithoutSave([NoEnumeration] IEnumerable<TModel> entities);


        Task Delete(TModel entity);
        Task Delete([NoEnumeration] IEnumerable<TModel> entities);

        void DeleteWithoutSave([NoEnumeration] IEnumerable<TModel> entities);
        void DeleteWithoutSave(TModel entity);

        Task<bool> Exist(TModelKeyId id, ContextReadOptions options = default);
        Task<bool> Exist(Expression<Func<TModel, bool>> expression, ContextReadOptions options = default);
        Task<bool> Exist(IEnumerable<Expression<Func<TModel, bool>>> expressions, ContextReadOptions options = default);
        Task<bool> Exist(IEnumerable<TModelKeyId> ids, ContextReadOptions options = default);
        IQueryable<TModel> Order(IQueryable<TModel> entities);
        void Detach(TModel entity);
        Task Save();
    }

    public class Context<TModel, TModelKeyId> : IContext<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId> where TModelKeyId : new()
    {
        private static readonly Type ModelKeyIdType = typeof(TModelKeyId);
        private readonly IDbContext _context;

        protected Context(IDbContext context)
        {
            _context = context;
        }

        public Context(IDbContext context, IQueryable<TModel> setQuery)
        {
            _context = context;
            SetQuery = setQuery;
        }

        protected virtual IQueryable<TModel> SetQuery { get; set; }

        /// <summary>
        ///     Adds entities to the set without calling the <c>Save</c> method.
        /// </summary>
        /// <param name="entities">Entities to be added to the set</param>
        /// <returns></returns>
        public virtual async Task CreateWithoutSave([NoEnumeration] IEnumerable<TModel> entities)
        {
            await _context.AddRangeAsync(entities);
        }

        /// <summary>
        ///     Adds single entity to the set without calling the <c>Save</c> method.
        /// </summary>
        /// <param name="entity">Entity to be added to the set</param>
        /// <returns></returns>
        public virtual async Task CreateWithoutSave(TModel entity)
        {
            await _context.AddAsync(entity);
        }

        /// <summary>
        ///     Adds the entities to the set and calls the <c>Save</c> method.
        /// </summary>
        /// <param name="entities">Entities to be added to the database</param>
        /// <returns>Entities after calling <c>Save</c> method</returns>
        public virtual async Task<IEnumerable<TModel>> Create([NoEnumeration] IEnumerable<TModel> entities)
        {
            await CreateWithoutSave(entities);
            await Save();
            return entities;
        }

        /// <summary>
        ///     Adds single entity to the set and calls the <c>Save</c> method.
        /// </summary>
        /// <param name="entity">Single entity to be added to the database</param>
        /// <returns>The entity after calling the <c>Save</c> method</returns>
        public virtual async Task<TModel> Create(TModel entity)
        {
            await _context.AddAsync(entity);
            await Save();
            Detach(entity);
            return entity;
        }

        /// <summary>
        ///     Finds one entity that matches the same supplied id <code>WHERE id = @id</code>
        /// </summary>
        /// <param name="id">Entity's Id of type <c>TModelKeyId</c></param>
        /// <param name="options">Read options</param>
        /// <returns>Entity that matches the id, null if no match</returns>
        public virtual async Task<TModel> FindOne(TModelKeyId id, ContextReadOptions options = null)
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
            ContextReadOptions options = null)
        {
            var query = Find(expression, options);
            if (query == default) return default;
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
        public virtual IQueryable<TModel> FindByIds(IEnumerable<TModelKeyId> ids, ContextReadOptions options)
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
            ContextReadOptions options = null)
        {
            return Find(new[] {expression}, options);
        }

        /// <summary>
        ///     generates a find query.
        /// </summary>
        /// <param name="options">Read options</param>
        /// <returns>find query</returns>
        public virtual IQueryable<TModel> Find(ContextReadOptions options = default)
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
            ContextReadOptions options = null)
        {
            return _find(expressions, options);
        }


        /// <summary>
        ///     updates entity
        /// </summary>
        /// <param name="entity">entity of type TModel</param>
        /// <returns>the updated entity</returns>
        public virtual async Task<TModel> Update(TModel entity)
        {
            // _context.entity(entity).State = EntityState.Modified;
            _context.Update(entity);
            await Save();
            return entity;
        }

        /// <summary>
        ///     updates entities
        /// </summary>
        /// <param name="entities">a collection of entities</param>
        /// <returns>updated entities</returns>
        public virtual async Task<IEnumerable<TModel>> Update([NoEnumeration] IEnumerable<TModel> entities)
        {
            _context.UpdateRange(entities);
            await Save();
            return entities;
        }

        /// <summary>
        ///     updates the entity without triggering the <c>Save</c> method (save the entity in memory)
        /// </summary>
        /// <param name="entity">the entity to be updated</param>
        public void UpdateWithoutSave(TModel entity)
        {
            _context.Update(entity);
            // _context.entity(entity).State = EntityState.Modified;
        }

        /// <summary>
        ///     updates the entities without triggering the <c>Save</c> method (save the entities in memory)
        /// </summary>
        /// <param name="entities">the entities to be updated</param>
        public void UpdateWithoutSave(IEnumerable<TModel> entities)
        {
            _context.UpdateRange(entities);
            // Set.UpdateRange(entities);
        }


        /// <summary>
        ///     deletes an entity
        /// </summary>
        /// <param name="entity">the entity to be deleted</param>
        public virtual async Task Delete(TModel entity)
        {
            _context.Remove(entity);
            await Save();
        }

        public virtual async Task Delete(IEnumerable<TModel> entities)
        {
            _context.RemoveRange(entities);
            await Save();
        }

        public virtual void DeleteWithoutSave(IEnumerable<TModel> entities)
        {
            _context.RemoveRange(entities);
        }

        public virtual void DeleteWithoutSave(TModel entity)
        {
            _context.Remove(entity);
        }


        public virtual void Detach(TModel entity)
        {
            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
        }

        public virtual async Task Save()
        {
            await _context.SaveChangesAsync();
        }

        public virtual Expression<Func<TModel, bool>> FindById(TModelKeyId id)
        {
            return _findById(id);
        }

        public virtual async Task<bool> Exist(TModelKeyId id, ContextReadOptions options = default)
        {
            return await Exist(_findById(id), options);
        }

        public virtual async Task<bool> Exist(Expression<Func<TModel, bool>> expression,
            ContextReadOptions options = default)
        {
            return await Exist(new[] {expression}, options);
        }

        public virtual async Task<bool> Exist(IEnumerable<TModelKeyId> ids,
            ContextReadOptions options = default)
        {
            if (options?.AllExist == true) return await Count(_findByIds(ids, options), options) == ids.LongCount();
            return await Exist(_findByIds(ids, options), options);
        }

        public virtual async Task<bool> Exist(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextReadOptions options = default)
        {
            return await AnyAsync(expressions, options);
        }


        public virtual IQueryable<TModel> Order(IQueryable<TModel> entities)
        {
            if (entities is IQueryable<DbEntity<TModelKeyId>> dbEntities)
                return dbEntities.OrderBy(x => x.CreationTime).Cast<TModel>();
            return entities;
        }

        private static Expression<Func<TModel, bool>> _findByIds(IEnumerable<TModelKeyId> ids,
            ContextReadOptions options)
        {
            var listIds = ids.ToList();
            if (ModelKeyIdType.IsPrimitive) return x => listIds.Contains(x.Id);
            {
                var normalizedIds = listIds.ToList();
                return x => normalizedIds.Contains(x.Id);
            }
        }


        protected virtual async Task<bool> AnyAsync(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextReadOptions options = default)
        {
            var query = Find(expressions, options);
            if (query == default) return default;
            return await query.AnyAsync();
        }

        protected virtual async Task<long> Count(Expression<Func<TModel, bool>> expression,
            ContextReadOptions options = default)
        {
            var query = Find(expression, options);
            if (query == default) return default;
            return await query.LongCountAsync();
        }

        protected virtual IQueryable<TModel> _read(ContextReadOptions options = default)
        {
            var set = SetQuery;
            if (options != default && options.Order) set = Order(set);
            if (options != default && options.Query) set = GetQueryProvider(set);
            return set;
        }

        protected virtual IQueryable<TModel> _find(IEnumerable<Expression<Func<TModel, bool>>> expressions,
            ContextReadOptions options = default)
        {
            options ??= new ContextReadOptions
            {
                Order = true,
                Query = true,
                Track = false
            };
            var set = _read(options);
            if (set == null) return default;
            if (expressions == null) return set;
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