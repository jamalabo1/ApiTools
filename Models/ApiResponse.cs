using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ApiTools.Models
{
    public interface IApiResponse
    {
        static readonly IApiResponse Ok = new ServiceResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true
        };

        static readonly IApiResponse NoContent = new ServiceResponse
        {
            StatusCode = StatusCodes.Status204NoContent,
            Success = true
        };

        static readonly IApiResponse BadRequest = new ServiceResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Success = false
        };

        static readonly IApiResponse UnAuthorized = new ServiceResponse
        {
            StatusCode = StatusCodes.Status401Unauthorized,
            Success = false
        };

        static readonly IApiResponse Forbidden = new ServiceResponse
        {
            StatusCode = StatusCodes.Status403Forbidden,
            Success = false
        };

        static readonly IApiResponse NotFound = new ServiceResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Success = false
        };

        static readonly IApiResponse InternalServerError = new ServiceResponse
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Success = false
        };

        public bool Success { get; set; }


        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public int StatusCode { get; set; }

        public IList<IApiResponseMessage> Messages { get; set; }
        IApiResponse<TO> ToOtherApiResponse<TO>(TO response = default);
    }
    public class ApiResponse : IApiResponse
    {
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]

        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public int StatusCode { get; set; }

        public IList<IApiResponseMessage> Messages { get; set; } =
            ImmutableList<IApiResponseMessage>.Empty;
        
        public virtual IApiResponse<TO> ToOtherApiResponse<TO>(TO response = default)
        {
            return new ApiResponse<TO>
            {
                Response = response,
                StatusCode = StatusCode,
                Messages = Messages,
                Success = Success,
            };
        }
    }

    public interface IApiResponse<T> : IApiResponse
    {
        T Response { get; set; }
    }

    public class ApiResponse<T> : ApiResponse, IApiResponse<T>
    {
        public T Response { get; set; }
    }
}