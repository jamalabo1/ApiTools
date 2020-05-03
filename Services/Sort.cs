using System.Linq;
using System.Reflection;
using ApiTools.Extensions;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Services
{
    public interface ISort
    {
        IQueryable<T> SortByKey<T>(IQueryable<T> set);
    }

    public class Sort : ISort
    {
        private readonly IHttpContextAccessor _accessor;

        public Sort(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public IQueryable<T> SortByKey<T>(IQueryable<T> set)
        {
            var query = _accessor.HttpContext.Request.Query;
            foreach (var (key, stringValues) in query)
                if (key == "sort")
                    for (var i = 0; i < stringValues.Count; i++)
                    {
                        var value = stringValues[i];
                        var desc = value.StartsWith("-");
                        var propName = value.Replace("-", "");
                        var propInfo = _propInfo<T>(propName);

                        if (propInfo == null) continue;
                        set = i == 0 ? set.OrderBy(propName, propInfo, desc) : set.ThenBy(propName, propInfo, desc);
                    }

            return set;
        }


        private static PropertyInfo _propInfo<T>(string propertyName)
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
    }
}