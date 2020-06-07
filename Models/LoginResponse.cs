using System;

namespace ApiTools.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string Status => "ok";
        public string AccountId { get; set; }
    }
}