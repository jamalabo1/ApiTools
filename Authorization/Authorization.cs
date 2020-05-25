using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApiTools.Extensions;
using ApiTools.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ApiTools.Authorization
{
    public class AuthorizationInfoContext<TEntityId>
    {
        public string UserRole { get; set; }
        public TEntityId UserId { get; set; }
    }

    public static class AuthorizationRoleRequirement
    {
        public const string DefaultRole = "@_default_role_";

        public static readonly IEnumerable<(OperationAuthorizationRequirement, bool)> NoDeleteOperation = new[]
        {
            (Operations.Delete, false)
        };

        public static readonly IEnumerable<(OperationAuthorizationRequirement, bool)> ReadOnlyOperation = new[]
        {
            (Operations.Read, true)
        };

        public static IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> MatchIdOnly<TEntity,
            TEntityId, TUserId>()
            where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            return new[]
            {
                MatchId<TEntity, TEntityId, TUserId>()
            };
        }

        public static Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>
            MatchId<TEntity, TEntityId, TUserId>()
            where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            return (entity, c) => Task.FromResult(entity.Id.Equals(c.UserId));
        }

        public static IAuthorizationRoleRequirement<TEntity, TUserId> NoDeleteOperationRequirement<TEntity, TEntityId,
            TUserId>(
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> requirements = null
        ) where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            requirements ??= MatchIdOnly<TEntity, TEntityId, TUserId>();

            return new AuthorizationRoleRequirement<TEntity, TUserId>(
                NoDeleteOperation,
                requirements,
                true
            );
        }

        public static IAuthorizationRoleRequirement<TEntity, TUserId> ReadOnlyOperationRequirement<TEntity, TEntityId,
            TUserId>(
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> requirements = null
        ) where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            requirements ??= MatchIdOnly<TEntity, TEntityId, TUserId>();

            return new AuthorizationRoleRequirement<TEntity, TUserId>(
                ReadOnlyOperation,
                requirements,
                true
            );
        }
    }

    public interface IAuthorizationRoleRequirement<in TEntity, TUserId>
    {
        IDictionary<OperationAuthorizationRequirement, bool> GetAllowedOperations();
        IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> GetValidationExpressions();
        bool GetDefaultRoleResult();
    }

    public class AuthorizationRoleRequirement<TEntity, TUserId> : IAuthorizationRoleRequirement<TEntity, TUserId>
    {
        public AuthorizationRoleRequirement(
            IEnumerable<(OperationAuthorizationRequirement, bool)> requirements,
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> validationExpressions,
            bool defaultRoleResult
        )
        {
            AllowedOperations = new Dictionary<OperationAuthorizationRequirement, bool>();
            foreach (var (operationAuthorizationRequirement, result) in requirements)
                AllowedOperations.Add(operationAuthorizationRequirement, result);

            ValidationExpressions = validationExpressions ??
                                    Enumerable.Empty<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>>();
            DefaultRoleResult = defaultRoleResult;
        }

        private bool DefaultRoleResult { get; }
        private IDictionary<OperationAuthorizationRequirement, bool> AllowedOperations { get; }

        private IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> ValidationExpressions { get; }

        public IDictionary<OperationAuthorizationRequirement, bool> GetAllowedOperations()
        {
            return AllowedOperations;
        }

        public bool GetDefaultRoleResult()
        {
            return DefaultRoleResult;
        }

        public IEnumerable<Func<TEntity, AuthorizationInfoContext<TUserId>, Task<bool>>> GetValidationExpressions()
        {
            return ValidationExpressions;
        }
    }

    public interface IAuthorizationRequirements<in TEntity, TUserId>
    {
        IAuthorizationRoleRequirement<TEntity, TUserId> this[string index] { get; }
    }

    public class AuthorizationRequirements<T, TUserId> : IAuthorizationRequirements<T, TUserId>
    {
        private readonly IDictionary<string, IAuthorizationRoleRequirement<T, TUserId>> _requirements;

        public AuthorizationRequirements(
            IEnumerable<(string, IAuthorizationRoleRequirement<T, TUserId>)> requirements
        )
        {
            _requirements = new Dictionary<string, IAuthorizationRoleRequirement<T, TUserId>>();
            foreach (var (item1, authorizationRoleRequirement) in requirements)
                _requirements.Add(item1, authorizationRoleRequirement);
        }

        public IAuthorizationRoleRequirement<T, TUserId> this[string index]
        {
            get
            {
                _requirements.TryGetValue(index, out var value);
                return value;
            }
        }
    }

    public abstract class
        Authorization<TEntity, TUserId> : AuthorizationHandler<OperationAuthorizationRequirement, TEntity>
    {
        protected abstract IAuthorizationRequirements<TEntity, TUserId> AuthorizationRequirements { get; set; }


        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            TEntity resource
        )
        {
            var userRole = context.User.GetUserRole();

            var roleRequirements = userRole == null
                ? AuthorizationRequirements[AuthorizationRoleRequirement.DefaultRole]
                : AuthorizationRequirements[userRole];


            if (roleRequirements != null)
            {
                var roleRequirementExists = roleRequirements.GetAllowedOperations().ContainsKey(requirement);

                if (!roleRequirementExists && roleRequirements.GetDefaultRoleResult() ||
                    roleRequirementExists && roleRequirements.GetAllowedOperations()[requirement])
                {
                    if (roleRequirements.GetValidationExpressions() != null)
                        foreach (var roleRequirementsValidationExpression in roleRequirements.GetValidationExpressions()
                        )
                        {
                            var userId = GetUserId(context.User);
                            if (!await roleRequirementsValidationExpression(resource,
                                new AuthorizationInfoContext<TUserId>
                                {
                                    UserId = userId,
                                    UserRole = userRole
                                }))
                                return;
                        }

                    context.Succeed(requirement);
                }
            }
        }

        protected abstract TUserId GetUserId(ClaimsPrincipal user);
    }
}