﻿using System;
using System.Security.Claims;

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

        public static string GetUserRole(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            var role = principal.FindFirst(ClaimTypes.Role);
            return role?.Value;
        }
    }
}