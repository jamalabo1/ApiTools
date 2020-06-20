﻿using System;
using System.Collections;
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
        private const int DefaultMaxNestingLevel = 2;

        public static PropertyInfo PropertyInfo<T>(string propertyName, bool enableNesting = true,
            int? maxNestingLevel = DefaultMaxNestingLevel,
            bool enableDashedNames = true)
        {
            var split = AccessPropertyFromName(propertyName, enableNesting, maxNestingLevel, enableDashedNames);
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


        public static PropertyInfo PropertyInfo(Type type, string propertyName, bool enableNesting = true,
            int? maxNestingLevel = DefaultMaxNestingLevel,
            bool enableDashedNames = true)
        {
            var split = AccessPropertyFromName(propertyName, enableNesting, maxNestingLevel, enableDashedNames).ToList();
            PropertyInfo propInfo = null;
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            for (var i = 0; i < split.Count; i++)
            {
                if (maxNestingLevel != null && maxNestingLevel > 0)
                    if (i + 1 > maxNestingLevel)
                        break;
                var p = split[i];
                if (propInfo == null)
                {
                    propInfo = type.GetProperty(p, flags);
                }
                else if (enableNesting)
                {
                    if (IsPropertyAList(propInfo))
                    {
                        var backingType = propInfo.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (backingType == null) break;
                        propInfo = backingType.GetProperty(p, flags);
                    }
                    else
                    {
                        propInfo = propInfo.PropertyType.GetProperty(p, flags);
                    }
                }
            }

            return propInfo;
        }

        public static bool IsAccessingAList<T>(string propertyName, bool enableNesting = true,
            int? maxNestingLevel = DefaultMaxNestingLevel,
            bool enableDashedNames = true)
        {
            var split = AccessPropertyFromName(propertyName, enableNesting, maxNestingLevel,
                enableDashedNames);
            PropertyInfo propInfo = null;
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            for (var i = 0; i < split.Count; i++)
            {
                if (maxNestingLevel != null && maxNestingLevel > 0)
                    if (i + 1 > maxNestingLevel)
                        break;
                var p = split[i];
                if (propInfo == null)
                {
                    propInfo = typeof(T).GetProperty(p, flags);
                }
                else if (enableNesting)
                {
                    if (IsPropertyAList(propInfo))
                        return true;
                    propInfo = propInfo.PropertyType.GetProperty(p, flags);
                }
                else
                {
                    if (IsPropertyAList(propInfo)) return true;
                }
            } 
            return IsPropertyAList(propInfo);
        }

        public static bool IsPropertyAList(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType.IsInterface &&
                   propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)) ||
                   propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IList));
        }

        public static List<string> AccessPropertyFromName(string propertyName,
            bool enableNesting = true,
            int? maxNestingLevel = DefaultMaxNestingLevel,
            bool enableDashedNames = true)
        {
            var split = propertyName.Split(".");
            if (enableNesting == false) maxNestingLevel = 1;
            if (maxNestingLevel != null && maxNestingLevel > 0) split = split.Take(maxNestingLevel.Value).ToArray();
            if (!enableDashedNames) return split.ToList();
            return split.Select(NormalizeDashed).ToList();
        }

        public static string NormalizeDashed(string propertyName)
        {
            var dashSplit = propertyName.Split("-");
            var normalizedName = new StringBuilder();
            foreach (var dashed in dashSplit)
                if (dashed != "" && dashed != string.Empty)
                    normalizedName.Append(dashed.FirstCharToUpper());

            return normalizedName.ToString();
        }

        public static (Expression, ParameterExpression) PropertyFunc<T>(string propertyName,
            bool enableNesting = true, int? maxNestingLevel = DefaultMaxNestingLevel, bool enableDashedNames = true)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var split = AccessPropertyFromName(propertyName, enableNesting, maxNestingLevel,
                enableDashedNames);

            var convertedParam = (Expression) param;
            var body = enableNesting
                ? split.Aggregate(convertedParam, Expression.PropertyOrField)
                : Expression.PropertyOrField(convertedParam, split.FirstOrDefault() ?? propertyName);

            return (body, param);
        }

        public static (Expression, ParameterExpression) PropertyFunc(Type type, string propertyName,
            bool enableNesting = true,
            int? maxNestingLevel = DefaultMaxNestingLevel,
            bool enableDashedNames = true)
        {
            var param = Expression.Parameter(type, "x");
            var split = AccessPropertyFromName(propertyName, enableNesting, maxNestingLevel, enableDashedNames);

            var convertedParam = (Expression) param;
            var body = enableNesting
                ? split.Aggregate(convertedParam, Expression.PropertyOrField)
                : Expression.PropertyOrField(convertedParam, split.FirstOrDefault() ?? propertyName);

            return (body, param);
        }
        public static Type BaseType(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsArray)
                return propertyInfo.PropertyType.GetElementType();
            if (IsPropertyAList(propertyInfo))
                return propertyInfo.PropertyType.GetGenericArguments().FirstOrDefault();
            return propertyInfo.PropertyType;
        }
    }
}