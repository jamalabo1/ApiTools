using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiTools.Helpers
{
    public class PropertyHelper
    {
        public static PropertyInfo PropertyInfo<T>(string propertyName, bool enableNesting = true)
        {
            var split = propertyName.Split(".");
            PropertyInfo propInfo = null;
            foreach (var p in split)
                if (propInfo == null)
                    propInfo = typeof(T).GetProperty(p,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                else if (enableNesting)
                    propInfo = propInfo.PropertyType.GetProperty(p,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            return propInfo;
        }


        public static Expression<Func<T, TProperty>> PropertyLambda<T, TProperty>(string propertyName,
            bool enableNesting = true)
        {
            var (body, param) = PropertyFunc<T>(propertyName, enableNesting);
            return (Expression<Func<T, TProperty>>) Expression.Lambda(body, param);
        }

        public static (Expression, ParameterExpression) PropertyFunc<T>(string propertyName,
            bool enableNesting = true)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var split = propertyName.Split(".");

            var convertedParam = (Expression) param;
            var body = enableNesting
                ? split.Aggregate(convertedParam, Expression.PropertyOrField)
                : Expression.PropertyOrField(convertedParam, split.FirstOrDefault() ?? propertyName);

            return (body, param);
        }
    }
}