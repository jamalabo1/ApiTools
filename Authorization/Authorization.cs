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

        public static IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> MatchIdOnly<TEntity,
            TEntityId>()
            where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            return new[]
            {
                MatchId<TEntity, TEntityId>()
            };
        }

        public static Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>> MatchId<TEntity, TEntityId>()
            where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            return (entity, c) => Task.FromResult(entity.Id.Equals(c.UserId));
        }

        public static IAuthorizationRoleRequirement<TEntity, TEntityId> NoDeleteOperationRequirement<TEntity, TEntityId>(
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> requirements = null
        ) where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            requirements ??= MatchIdOnly<TEntity, TEntityId>();

            return new AuthorizationRoleRequirement<TEntity, TEntityId>(
                NoDeleteOperation,
                requirements,
                true
            );
        }

        public static IAuthorizationRoleRequirement<TEntity, TEntityId> ReadOnlyOperationRequirement<TEntity, TEntityId>(
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> requirements = null
        ) where TEntity : ContextEntity<TEntityId> where TEntityId : new()
        {
            requirements ??= MatchIdOnly<TEntity, TEntityId>();

            return new AuthorizationRoleRequirement<TEntity, TEntityId>(
                ReadOnlyOperation,
                requirements,
                true
            );
        }
    }

    public interface IAuthorizationRoleRequirement<in TEntity, TEntityId>
    {
        IDictionary<OperationAuthorizationRequirement, bool> GetAllowedOperations();
        IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> GetValidationExpressions();
        bool GetDefaultRoleResult();
    }

    public class AuthorizationRoleRequirement<TEntity, TEntityId> : IAuthorizationRoleRequirement<TEntity, TEntityId>
    {
        public AuthorizationRoleRequirement(
            IEnumerable<(OperationAuthorizationRequirement, bool)> requirements,
            IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> validationExpressions,
            bool defaultRoleResult
        )
        {
            AllowedOperations = new Dictionary<OperationAuthorizationRequirement, bool>();
            foreach (var (operationAuthorizationRequirement, result) in requirements)
                AllowedOperations.Add(operationAuthorizationRequirement, result);

            ValidationExpressions = validationExpressions ??
                                    Enumerable.Empty<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>>();
            DefaultRoleResult = defaultRoleResult;
        }

        private bool DefaultRoleResult { get; }
        private IDictionary<OperationAuthorizationRequirement, bool> AllowedOperations { get; }

        private IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> ValidationExpressions
        {
            get;
        }

        public IDictionary<OperationAuthorizationRequirement, bool> GetAllowedOperations()
        {
            return AllowedOperations;
        }

        public bool GetDefaultRoleResult()
        {
            return DefaultRoleResult;
        }

        public IEnumerable<Func<TEntity, AuthorizationInfoContext<TEntityId>, Task<bool>>> GetValidationExpressions()
        {
            return ValidationExpressions;
        }
    }

    public interface IAuthorizationRequirements<T, TEntityId>
    {
        IAuthorizationRoleRequirement<T, TEntityId> this[string index] { get; }
    }

    public class AuthorizationRequirements<T, TEntityId> : IAuthorizationRequirements<T, TEntityId>
    {
        private readonly IDictionary<string, IAuthorizationRoleRequirement<T, TEntityId>> _requirements;

        public AuthorizationRequirements(
            IEnumerable<(string, IAuthorizationRoleRequirement<T, TEntityId>)> requirements
        )
        {
            _requirements = new Dictionary<string, IAuthorizationRoleRequirement<T, TEntityId>>();
            foreach (var (item1, authorizationRoleRequirement) in requirements)
                _requirements.Add(item1, authorizationRoleRequirement);
        }

        public IAuthorizationRoleRequirement<T, TEntityId> this[string index]
        {
            get
            {
                _requirements.TryGetValue(index, out var value);
                return value;
            }
        }
    }

    public abstract class
        Authorization<TEntity, TEntityId> : AuthorizationHandler<OperationAuthorizationRequirement, TEntity>
    {
        protected abstract IAuthorizationRequirements<TEntity, TEntityId> AuthorizationRequirements { get; set; }


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
                                new AuthorizationInfoContext<TEntityId>
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

        protected abstract TEntityId GetUserId(ClaimsPrincipal user);
    }
}