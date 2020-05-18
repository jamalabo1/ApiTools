using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ApiTools.Extensions;

namespace ApiTools.Helpers
{
    public class PropertyHelper
    {
        public static PropertyInfo PropertyInfo<T>(string propertyName, bool enableNesting = true,
            bool enableDashedNames = true)
        {
            var split = AccessPropertyFromName(propertyName, enableDashedNames);
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

        private static IEnumerable<string> AccessPropertyFromName(string propertyName,
            bool enableDashedNames = true)
        {
            IEnumerable<string> split;

            if (enableDashedNames)
            {
                var dashSplit = propertyName.Split("-");
                var normalizedName = new StringBuilder();
                foreach (var dashed in dashSplit)
                    if (dashed != "" && dashed != string.Empty)
                        normalizedName.Append(dashed.FirstCharToUpper());
                split = new[] {normalizedName.ToString()};
            }
            else
            {
                split = propertyName.Split(".");
            }

            return split;
        }


        public static Expression<Func<T, TProperty>> PropertyLambda<T, TProperty>(string propertyName,
            bool enableNesting = true)
        {
            var (body, param) = PropertyFunc<T>(propertyName, enableNesting);
            return (Expression<Func<T, TProperty>>) Expression.Lambda(body, param);
        }

        public static (Expression, ParameterExpression) PropertyFunc<T>(string propertyName,
            bool enableNesting = true, bool enableDashedNames = true)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var split = AccessPropertyFromName(propertyName, enableDashedNames);

            var convertedParam = (Expression) param;
            var body = enableNesting
                ? split.Aggregate(convertedParam, Expression.PropertyOrField)
                : Expression.PropertyOrField(convertedParam, split.FirstOrDefault() ?? propertyName);

            return (body, param);
        }
    }
}