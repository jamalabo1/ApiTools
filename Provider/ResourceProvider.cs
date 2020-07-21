using System;
using System.Linq;
using ApiTools.Extensions;
using ApiTools.Services;

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

        public ResourceQueryProvider(IServiceHelper serviceHelper)
        {
            var user = serviceHelper.Accessor.HttpContext.User;
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