using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiTools.Helpers
{
    public class PropertyHelper
    {
        public static PropertyInfo PropertyInfo<T>(string propertyName)
        {
            var split = propertyName.Split(".");
            PropertyInfo propInfo = null;
            foreach (var p in split)
                if (propInfo == null)
                    propInfo = typeof(T).GetProperty(p,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                else
                    propInfo = propInfo.PropertyType.GetProperty(p,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            return propInfo;
        }

        public static Expression<Func<T, object>> PropertyFunc<T>(string propertyName)
        {
            Expression param = Expression.Parameter(typeof(T), "x");
            var split = propertyName.Split(".");
            var body = split.Aggregate(param, Expression.PropertyOrField);
            // x.{propertyName}
            var func = Expression.Lambda<Func<T, object>>(body);
            return func;
        }
    }
}