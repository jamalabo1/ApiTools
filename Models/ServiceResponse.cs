using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ApiTools.Models
{
    public interface IServiceResponse
    {
        static readonly IServiceResponse Ok = new ServiceResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true
        };

        static readonly IServiceResponse NoContent = new ServiceResponse
        {
            StatusCode = StatusCodes.Status204NoContent,
            Success = true
        };

        static readonly IServiceResponse BadRequest = new ServiceResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Success = false
        };

        static readonly IServiceResponse UnAuthorized = new ServiceResponse
        {
            StatusCode = StatusCodes.Status401Unauthorized,
            Success = false
        };

        static readonly IServiceResponse Forbidden = new ServiceResponse
        {
            StatusCode = StatusCodes.Status403Forbidden,
            Success = false
        };

        static readonly IServiceResponse NotFound = new ServiceResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Success = false
        };

        static readonly IServiceResponse InternalServerError = new ServiceResponse
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Success = false
        };

        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool TriggerSave { get; set; }

        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public int StatusCode { get; set; }

        public IList<IServiceResponseMessage> Messages { get; set; }
        public IServiceResponse<TO> ToOtherResponse<TO>(TO response = default);
    }

    public class ServiceResponse : IServiceResponse
    {
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool TriggerSave { get; set; } = true;

        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public int StatusCode { get; set; }

        public IList<IServiceResponseMessage> Messages { get; set; } =
            ImmutableList<IServiceResponseMessage>.Empty;

        public virtual IServiceResponse<TO> ToOtherResponse<TO>(TO response = default)
        {
            return new ServiceResponse<TO>
            {
                Response = response,
                StatusCode = StatusCode,
                Messages = Messages,
                Success = Success,
                TriggerSave = TriggerSave
            };
        }
    }

    public interface IServiceResponse<T> : IServiceResponse
    {
        // public ServiceResponse<TO> ToOtherResponse<TO>();
        public static readonly IServiceResponse<T> Ok = IServiceResponse.Ok.ToOtherResponse<T>();
        public static readonly IServiceResponse<T> NoContent = IServiceResponse.NoContent.ToOtherResponse<T>();
        public static readonly IServiceResponse<T> BadRequest = IServiceResponse.BadRequest.ToOtherResponse<T>();
        public static readonly IServiceResponse<T> UnAuthorized = IServiceResponse.UnAuthorized.ToOtherResponse<T>();
        public static readonly IServiceResponse<T> Forbidden = IServiceResponse.Forbidden.ToOtherResponse<T>();
        public static readonly IServiceResponse<T> NotFound = IServiceResponse.NotFound.ToOtherResponse<T>();

        public static readonly IServiceResponse<T> InternalServerError =
            IServiceResponse.InternalServerError.ToOtherResponse<T>();

        public T Response { get; set; }
    }

    public class ServiceResponse<T> : ServiceResponse, IServiceResponse<T>
    {
        public T Response { get; set; }


        // public ServiceResponse<TO> ToOtherResponse<TO>()
        // {
        //     return base.ToOtherResponse<TO>();
        // }
    }
}