using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ApiTools.Models
{
    public class ServiceResponse
    {
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonIgnore] public bool TriggerSave { get; set; } = true;

        [JsonPropertyName("status")] 
        [JsonProperty("status")]
        public int StatusCode { get; set; }

        public IList<ServiceResponseMessage> Messages { get; set; } =
            ImmutableList<ServiceResponseMessage>.Empty;
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        public static readonly ServiceResponse<T> Ok = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true
        };

        public static readonly ServiceResponse<T> NoContent = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status204NoContent,
            Success = false
        };

        public static readonly ServiceResponse<T> BadRequest = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Success = false
        };

        public static readonly ServiceResponse<T> UnAuthorized = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status401Unauthorized,
            Success = false
        };

        public static readonly ServiceResponse<T> Forbidden = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status403Forbidden,
            Success = false
        };

        public static readonly ServiceResponse<T> NotFound = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status404NotFound,
            Success = false
        };

        public static readonly ServiceResponse<T> InternalServerError = new ServiceResponse<T>
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Success = false
        };


        public T Response { get; set; }


        public static ServiceResponse<T> FromOtherResponse<TOther>(ServiceResponse<TOther> otherResponse)
        {
            ServiceResponse response = otherResponse;
            return (ServiceResponse<T>) response;
        }

        public static ServiceResponse<T> FromOtherResponse(ServiceResponse otherResponse)
        {
            return (ServiceResponse<T>) otherResponse;
        }
    }
}