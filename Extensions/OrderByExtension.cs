using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiTools.Extensions
{
    public static class OrderByExtension
    {
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty,
            PropertyInfo property,
            bool desc)
        {
            var command = desc ? "OrderByDescending" : "OrderBy";
            return CreateQuery(source, orderByProperty, property, command);
        }

        public static IQueryable<TEntity> ThenBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty,
            PropertyInfo property,
            bool desc)
        {
            var command = desc ? "ThenByDescending" : "ThenBy";
            return CreateQuery(source, orderByProperty, property, command);
        }

        private static IQueryable<TEntity> CreateQuery<TEntity>(IQueryable<TEntity> source,
            string propertyName, PropertyInfo property, string command)
        {
            var type = typeof(TEntity);
            var param = Expression.Parameter(type, "x");
            var body = propertyName.Split('.').Aggregate<string, Expression>(param, Expression.PropertyOrField);
            var resultExpression = Expression.Call(typeof(Queryable), command, new[] {type, property.PropertyType},
                source.Expression, Expression.Quote(Expression.Lambda(body, param)));
            return source.Provider.CreateQuery<TEntity>(resultExpression);
        }
    }
}