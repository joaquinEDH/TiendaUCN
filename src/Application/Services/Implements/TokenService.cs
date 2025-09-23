using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;

namespace Tienda.src.Application.Services.Implements
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user, string roleName, bool rememberMe)
        {
            var jwtSecret = _config["JWTSecret"]
                ?? throw new InvalidOperationException("La clave secreta JWT no está configurada.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(ClaimTypes.Role, roleName)
            };

            var expires = DateTime.UtcNow.AddHours(rememberMe ? 24 : 1);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}