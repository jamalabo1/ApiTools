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
        Task<ServiceResponse<LoginResponse>> Authenticate<TModel, TModelId>(
            IContext<TModel, TModelId> context,
            Expression<Func<TModel, bool>> expression,
            string password,
            string role,
            Func<TModel, ServiceResponse<LoginResponse>> validate = default
        )
            where TModel : AccountDbEntity<TModelId> where TModelId : new();
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
            Expression<Func<TModel, bool>> expression,
            string password,
            string role,
            Func<TModel, ServiceResponse<LoginResponse>> validate = default)
            where TModel : AccountDbEntity<TModelId>
            where TModelId : new()
        {
            var entity =
                await context.FindOne(
                    expression,
                    ContextReadOptions.DisableQuery);
            if (entity == null)
                return new ServiceResponse<LoginResponse>
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

            if (validate != default)
            {
                var validationResponse = validate(entity);
                if (!validationResponse.Success) return validationResponse;
            }

            if (!_passwordService.ValidatePassword(password, entity.Password))
                return new ServiceResponse<LoginResponse>
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
            var token = _tokenService.GenerateToken(entity.Id.ToString(), role);
            return _resp(token, role, entity.Id);
        }


        private static ServiceResponse<LoginResponse> _resp<TModelId>(string token, string role, TModelId accountId)
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