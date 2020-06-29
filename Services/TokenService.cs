using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiTools.Services
{
    public interface ITokenService
    {
         string GenerateToken(ClaimsIdentity claimsIdentity);
         ClaimsIdentity GenerateClaims(string id, string role);
    }

    public abstract class TokenService : ITokenService
    {
        protected virtual JwtSecurityTokenHandler Handler { get; set; }=new JwtSecurityTokenHandler();
        protected abstract string Secret { get; set; }
        protected abstract TimeSpan ExpireAt { get; set; }

        protected virtual string Issuer { get; set; } = null;

        protected virtual byte[] Key => Encoding.ASCII.GetBytes(Secret); 

        public virtual string GenerateToken(ClaimsIdentity claimsIdentity)
        {
            var tokenDescriptor = GenerateDescriptor(claimsIdentity);
            var token = Handler.CreateToken(tokenDescriptor);
            return Handler.WriteToken(token);
        }

        protected virtual SecurityTokenDescriptor GenerateDescriptor(ClaimsIdentity claimsIdentity)
        {
            return new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                IssuedAt = DateTime.Now,
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.Add(ExpireAt),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
        }
        
        public virtual ClaimsIdentity GenerateClaims(string id, string role)
        {
            return new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Role, role)
            });
        }
    }
}