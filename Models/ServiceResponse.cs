namespace ApiTools.Models
{
    public class ServiceResponse
    {
        public bool Success = true;
        public bool TriggerSave { get; set; } = true;
        public int StatusCode { get; set; }
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        public static readonly ServiceResponse<T> Ok = new ServiceResponse<T> {StatusCode = 200};

        public static readonly ServiceResponse<T> NoContent = new ServiceResponse<T>
            {StatusCode = 204, Success = false};

        public static readonly ServiceResponse<T> BadRequest = new ServiceResponse<T>
            {StatusCode = 400, Success = false};

        public static readonly ServiceResponse<T> UnAuthorized = new ServiceResponse<T>
            {StatusCode = 401, Success = false};

        public static readonly ServiceResponse<T> Forbidden = new ServiceResponse<T>
            {StatusCode = 403, Success = false};

        public static readonly ServiceResponse<T> NotFound = new ServiceResponse<T> {StatusCode = 404, Success = false};

        public static readonly ServiceResponse<T> InternalServerError = new ServiceResponse<T>
            {StatusCode = 500, Success = false};


        public T Response { get; set; }


        public static ServiceResponse<T> FromOtherResponse<TOther>(ServiceResponse<TOther> otherResponse)
        {
            var response = (ServiceResponse) otherResponse;
            return (ServiceResponse<T>) response;
        }
    }
}