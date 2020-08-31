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
        Task<IServiceResponse<ILoginResponse>> Authenticate<TModel, TModelId, TKey>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options,
            Expression<Func<TModel, TKey>> keySelector = null
        ) where TModel : IBaseAccountDbEntity<TModelId> where TModelId : new();

        Task<IServiceResponse<ILoginResponse>> Authenticate<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options
        )
            where TModel : IBaseAccountDbEntity<TModelId> where TModelId : new();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthenticationService(IServiceHelper serviceHelper)
        {
            _passwordService = serviceHelper.PasswordService;
            _tokenService = serviceHelper.TokenService;
        }

        public async Task<IServiceResponse<ILoginResponse>> Authenticate<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options)
            where TModel : IBaseAccountDbEntity<TModelId>
            where TModelId : new()
        {
            return await Authenticate<TModel, TModelId, Guid>(context, options);
        }

        public async Task<IServiceResponse<ILoginResponse>> Authenticate<TModel, TModelId, TKey>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options,
            Expression<Func<TModel, TKey>> keySelector = null
        )
            where TModel : IBaseAccountDbEntity<TModelId>
            where TModelId : new()
        {
            var validateLoginResponse = await ValidateLogin(context, options);
            if (!validateLoginResponse.Success)
                return validateLoginResponse.ToOtherServiceResponse<ILoginResponse>();

            var entity = validateLoginResponse.Response;
            var id = keySelector == null ? entity.Id.ToString() : keySelector.Compile().Invoke(entity).ToString();
            var token = _tokenService.GenerateToken(
                _tokenService.GenerateClaims(id, options.Role)
            );
            return _resp(token, options.Role, entity.Id);
        }


        protected virtual async Task<IServiceResponse<TModel>> ValidateLogin<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            AuthenticationOptions<TModel, TModelId> options
        ) where TModel : IBaseAccountDbEntity<TModelId>
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
                    Messages = new IApiResponseMessage[]
                    {
                        new ApiResponseMessage
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
                    if (!validationResponse.Success) return validationResponse.ToOtherServiceResponse<TModel>();
                }

            if (!_passwordService.ValidatePassword(options.Password, entity.Password))
                return new ServiceResponse<TModel>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Messages = new IApiResponseMessage[]
                    {
                        new ApiResponseMessage
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


        protected virtual ServiceResponse<ILoginResponse> _resp<TModelId>(string token, string role, TModelId accountId)
        {
            var resp = new LoginResponse
            {
                Token = token,
                Role = role,
                AccountId = accountId.ToString()
            };
            return new ServiceResponse<ILoginResponse>
            {
                Response = resp,
                StatusCode = StatusCodes.Status200OK,
                Success = true
            };
        }
    }
}