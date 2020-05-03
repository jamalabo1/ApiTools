﻿using System;
using System.Security.Claims;

namespace ApiTools.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            var id = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (id == null) return null;
            return Guid.Parse(id.Value);
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