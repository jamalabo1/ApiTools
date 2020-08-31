namespace ApiTools.Models
{
    public enum MessageType
    {
        Success = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }

    public interface IApiResponseMessage
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public MessageType Type { get; set; }
        public bool IsError => Type == MessageType.Error;
    }
    public class ApiResponseMessage : IApiResponseMessage
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public MessageType Type { get; set; }
        public bool IsError => Type == MessageType.Error;
    }
}