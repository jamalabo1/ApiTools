namespace ApiTools.Models
{
    public interface ILoginResponse
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string Status => "ok";
        public string AccountId { get; set; }
    }

    public class LoginResponse : ILoginResponse
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string Status => "ok";
        public string AccountId { get; set; }
    }
}