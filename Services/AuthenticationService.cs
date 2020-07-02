using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiTools.Context;
using ApiTools.Models;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Services
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse<LoginResponse>> Authenticate<TModel, TModelId, TKey>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options,
            Expression<Func<TModel, TKey>> keySelector = null
        )  where TModel : IAccountDbEntity<TModelId> where TModelId : new();

        Task<ServiceResponse<LoginResponse>> Authenticate<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options
        )
            where TModel : IAccountDbEntity<TModelId> where TModelId : new();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthenticationService(ITokenService tokenService, IPasswordService passwordService)
        {
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task<ServiceResponse<LoginResponse>> Authenticate<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options)
            where TModel : IAccountDbEntity<TModelId>
            where TModelId : new()
        {
            return await Authenticate<TModel, TModelId, Guid>(context, options);
        }


        protected virtual async Task<ServiceResponse<TModel>> ValidateLogin<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options
        )     where TModel : IAccountDbEntity<TModelId>
            where TModelId : new()
        {
            var entity = await (options.ContextFind != null
                ? options.ContextFind(context)
                : context.FindOne(
                    options.Expression,
                    ContextOptions.DisableQuery));
            
            if (entity == null)
                return new ServiceResponse<TModel>
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Messages = new[]
                    {
                        new ServiceResponseMessage
                        {
                            Code = "authentication.authenticate.not-found",
                            Message = "Account was not found",
                            Type = MessageType.Error
                        }
                    },
                    Success = false
                };

            if (options.ValidationOptions != null)
                foreach (var optionsValidationOption in options.ValidationOptions)
                {
                    var validationResponse = optionsValidationOption(entity);
                    if (!validationResponse.Success) return ServiceResponse<TModel>.FromOtherResponse(validationResponse);
                }

            if (!_passwordService.ValidatePassword(options.Password, entity.Password))
                return new ServiceResponse<TModel>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Messages = new[]
                    {
                        new ServiceResponseMessage
                        {
                            Code = "authentication.authenticate.password-mismatch",
                            Message = "The entered password does not match.",
                            Type = MessageType.Error
                        }
                    },
                    Success = false
                };
            return new ServiceResponse<TModel>
            {
                Response = entity,
                Success = true
            };
        }
        public async Task<ServiceResponse<LoginResponse>> Authenticate<TModel, TModelId, TKey>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options,
            Expression<Func<TModel, TKey>> keySelector = null
        )
            where TModel : IAccountDbEntity<TModelId>
            where TModelId : new()
        {
            var validateLoginResponse = await ValidateLogin(context, options);
            if (!validateLoginResponse.Success)
            {
                return ServiceResponse<LoginResponse>.FromOtherResponse(validateLoginResponse);
            }
            var entity = validateLoginResponse.Response;
            var id = keySelector == null ? entity.Id.ToString() : keySelector.Compile().Invoke(entity).ToString();
            var token = _tokenService.GenerateToken(
                _tokenService.GenerateClaims(id, options.Role)
            );
            return _resp(token, options.Role, entity.Id);
        }


        protected virtual ServiceResponse<LoginResponse> _resp<TModelId>(string token, string role, TModelId accountId)
        {
            var resp = new LoginResponse
            {
                Token = token,
                Role = role,
                AccountId = accountId.ToString()
            };
            return new ServiceResponse<LoginResponse>
            {
                Response = resp,
                StatusCode = StatusCodes.Status200OK,
                Success = true
            };
        }
    }
}