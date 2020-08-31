using Microsoft.AspNetCore.Http;

namespace ApiTools.Models
{
    public interface IServiceResponse : IApiResponse
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
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool TriggerSave { get; set; }
        IServiceResponse<TO> ToOtherServiceResponse<TO>(TO response = default);
    }

    public class ServiceResponse : ApiResponse, IServiceResponse
    {
        public bool TriggerSave { get; set; } = true;

        public virtual IServiceResponse<TO> ToOtherServiceResponse<TO>(TO response = default)
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

    public interface IServiceResponse<T> : IServiceResponse, IApiResponse<T>
    {
        // public ServiceResponse<TO> ToOtherResponse<TO>();
        static readonly IServiceResponse<T> Ok = IServiceResponse.Ok.ToOtherServiceResponse<T>();
        static readonly IServiceResponse<T> NoContent = IServiceResponse.NoContent.ToOtherServiceResponse<T>();
        static readonly IServiceResponse<T> BadRequest = IServiceResponse.BadRequest.ToOtherServiceResponse<T>();
        static readonly IServiceResponse<T> UnAuthorized = IServiceResponse.UnAuthorized.ToOtherServiceResponse<T>();
        static readonly IServiceResponse<T> Forbidden = IServiceResponse.Forbidden.ToOtherServiceResponse<T>();
        static readonly IServiceResponse<T> NotFound = IServiceResponse.NotFound.ToOtherServiceResponse<T>();

        static readonly IServiceResponse<T> InternalServerError =
            IServiceResponse.InternalServerError.ToOtherServiceResponse<T>();
    }

    public class ServiceResponse<T> : ServiceResponse, IServiceResponse<T>
    {
        public T Response { get; set; }
    }
}