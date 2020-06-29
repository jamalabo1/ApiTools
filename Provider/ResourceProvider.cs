using System;
using System.Linq;
using ApiTools.Extensions;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Provider
{
    public interface IResourceQueryProvider
    {
        IQueryable<T> GetQuery<T>(IQueryable<T> set);
    }

    public class ResourceQueryProvider : IResourceQueryProvider
    {
        protected readonly Guid UserId;
        protected readonly string UserRole;

        public ResourceQueryProvider(IHttpContextAccessor accessor)
        {
            var user = accessor?.HttpContext?.User;
            if (user == null) return;
            UserId = user.GetUserId();
            UserRole = user.GetUserRole();
        }

        public virtual IQueryable<T> GetQuery<T>(IQueryable<T> set)
        {
            return set;
        }
    }
}