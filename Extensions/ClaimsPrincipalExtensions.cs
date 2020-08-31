using System;
using System.Security.Claims;
using ApiTools.Models;
using JetBrains.Annotations;

namespace ApiTools.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            var id = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (id == null || id.Value == string.Empty) return Guid.Empty;
            return Guid.TryParse(id.Value, out var gid) ? gid : Guid.Empty;
        }

        [CanBeNull]
        public static string GetUserRole(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            var role = principal.FindFirst(ClaimTypes.Role);
            return role?.Value ?? AuthorizationRoles.Anonymous;
        }
    }
}