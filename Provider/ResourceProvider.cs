using System;
using System.Linq;
using ApiTools.Extensions;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Provider
{
    public interface IResourceQueryProvider
    {
        IQueryable<T> GetSet<T>(IQueryable<T> set);
    }

    public class ResourceQueryProvider : IResourceQueryProvider
    {
        private readonly Guid _userId;
        private readonly string _userRole;

        public ResourceQueryProvider(IHttpContextAccessor accessor)
        {
            var user = accessor?.HttpContext?.User;
            if (user == null) return;
            _userId = user.GetUserId();
            _userRole = user.GetUserRole();
        }

        public IQueryable<T> GetSet<T>(IQueryable<T> set)
        {
            return set;
        }
    }
}