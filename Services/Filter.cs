using System.Linq;
using ApiTools.Extensions;
using ApiTools.Models;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Services
{
    public class Filter : IFilter
    {
        public Filter(IServiceHelper serviceHelper)
        {
            QueryCollection = serviceHelper?.Accessor?.HttpContext?.Request?.Query;
            UserRole = serviceHelper?.Accessor?.HttpContext?.User.GetUserRole();
        }

        protected IQueryCollection QueryCollection { get; set; }
        protected string UserRole { get; set; }

        public virtual IQueryable<T> ApplyFilter<T>(IQueryable<T> query)
        {
            return query;
        }
    }
}