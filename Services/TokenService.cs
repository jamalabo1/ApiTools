using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiTools.Services
{
    public interface ITokenService
    {
        public string GenerateToken(string id, string role);
    }

    public abstract class TokenService : ITokenService
    {
        protected abstract string Secret { get; set; }
        protected abstract TimeSpan ExpireAt { get; set; }

        protected virtual string Issuer { get; set; } = null;

        public virtual string GenerateToken(string id, string role)
        {
            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                IssuedAt = DateTime.Now,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, id),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.Add(ExpireAt),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}