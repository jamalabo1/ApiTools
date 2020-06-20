namespace ApiTools.Models
{
    public class AzureFunctionRequest<T>
    {
        public T Data { get; set; }
        public string AuthorizationToken { get; set; }
    }
}