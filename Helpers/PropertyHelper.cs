using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
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
            return PropertyInfo(typeof(T), propertyName, enableNesting, maxNestingLevel, enableDashedNames);
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
            return IsPropertyAList(propertyInfo.PropertyType);
        }
        public static bool IsPropertyAList(Type propertyInfo)
        {
            return propertyInfo.IsInterface &&
                   propertyInfo.GetInterfaces().Contains(typeof(IEnumerable)) ||
                   propertyInfo.GetInterfaces().Contains(typeof(IList));
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
        public static Type BaseType(Type propertyInfo)
        {
            if (propertyInfo.IsArray)
                return propertyInfo.GetElementType();
            if (IsPropertyAList(propertyInfo))
                return propertyInfo.GetGenericArguments().FirstOrDefault();
            return propertyInfo;
        }
        public static void EmptyRelationalData<T>(IEnumerable<T> entities)
        {
            var virtualProperties = typeof(T)
                .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetAccessors().Any(r => r.IsVirtual));
            foreach (var property in virtualProperties)
            {
                if (property.PropertyType.IsValueType &&
                    Nullable.GetUnderlyingType(property.PropertyType) == null) continue;
                foreach (var contextEntity in entities) property.SetValue(contextEntity, null);
            }
        }
        
        public static Type GetModelIdType(Type type)
        {
            var baseType = GetBaseType(type);
            return baseType.GetGenericArguments()[0];
        }

        public static Type GetBaseType(Type type)
        {
            var baseType = type.BaseType;
            if (baseType == null || baseType.BaseType == null) return type;
            while (baseType.BaseType?.BaseType != null) baseType = baseType.BaseType;
            return baseType;
        }
    }
    public static class TypeBuilderHelper
{
    /// <summary>Creates one constructor for each public constructor in the base class. Each constructor simply
    /// forwards its arguments to the base constructor, and matches the base constructor's signature.
    /// Supports optional values, and custom attributes on constructors and parameters.
    /// Does not support n-ary (variadic) constructors</summary>
    public static void CreatePassThroughConstructors(this TypeBuilder builder, Type baseType)
    {
        foreach (var constructor in baseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            var parameters = constructor.GetParameters();
            if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false)) {
                //throw new InvalidOperationException("Variadic constructors are not supported");
                continue;
            }

            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var requiredCustomModifiers = parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray();
            var optionalCustomModifiers = parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray();

            var ctor = builder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            for (var i = 0; i < parameters.Length; ++i) {
                var parameter = parameters[i];
                var parameterBuilder = ctor.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0) {
                    parameterBuilder.SetConstant(parameter.RawDefaultValue);
                }

                foreach (var attribute in BuildCustomAttributes(parameter.GetCustomAttributesData())) {
                    parameterBuilder.SetCustomAttribute(attribute);
                }
            }

            foreach (var attribute in BuildCustomAttributes(constructor.GetCustomAttributesData())) {
                ctor.SetCustomAttribute(attribute);
            }

            var emitter = ctor.GetILGenerator();
            emitter.Emit(OpCodes.Nop);

            // Load `this` and call base constructor with arguments
            emitter.Emit(OpCodes.Ldarg_0);
            for (var i = 1; i <= parameters.Length; ++i) {
                emitter.Emit(OpCodes.Ldarg, i);
            }
            emitter.Emit(OpCodes.Call, constructor);

            emitter.Emit(OpCodes.Ret);
        }
    }


    private static CustomAttributeBuilder[] BuildCustomAttributes(IEnumerable<CustomAttributeData> customAttributes)
    {
        return customAttributes.Select(attribute => {
            var attributeArgs = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
            var namedPropertyInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
            var namedPropertyValues = attribute.NamedArguments.Where(a => a.MemberInfo is PropertyInfo).Select(a => a.TypedValue.Value).ToArray();
            var namedFieldInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<FieldInfo>().ToArray();
            var namedFieldValues = attribute.NamedArguments.Where(a => a.MemberInfo is FieldInfo).Select(a => a.TypedValue.Value).ToArray();
            return new CustomAttributeBuilder(attribute.Constructor, attributeArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues);
        }).ToArray();
    }
}
    
}