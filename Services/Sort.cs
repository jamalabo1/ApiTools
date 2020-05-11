using System.Linq;
using System.Reflection;
using ApiTools.Extensions;
using ApiTools.Helpers;
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
                        var propInfo = PropertyHelper.PropertyInfo<T>(propName);

                        if (propInfo == null) continue;
                        set = i == 0 ? set.OrderBy(propName, propInfo, desc) : set.ThenBy(propName, propInfo, desc);
                    }

            return set;
        }


 
    }
}