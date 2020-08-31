using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ApiTools.Models;

namespace ApiTools.Extensions
{
    public static class QueryableExtension
    {
        public static Expression<Func<T, bool>> FindByIdsExpression<TId, T>(IEnumerable<TId> ids) where T : IContextEntity<TId> where TId : new()
        {
            var listIds = ids.ToList();
            return x => listIds.Contains(x.Id);
        }
        public static IQueryable<T> FindByIds<TId, T>(this IQueryable<T> queryable, IEnumerable<TId> ids) where T : IContextEntity<TId> where TId : new()
        {
            return queryable.Where(FindByIdsExpression<TId, T>(ids));
        }
    }
}