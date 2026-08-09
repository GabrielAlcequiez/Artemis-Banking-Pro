using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ABP.Infrastructure.Identity.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private const string CommerceIdClaim = "CommerceId";

        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateToken(TokenGenerationRequest request)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, request.UserId),
                new(JwtRegisteredClaimNames.UniqueName, request.UserName),
                new(ClaimTypes.Role, request.Role)
            };

            if (request.CommerceId.HasValue)
            {
                claims.Add(new Claim(CommerceIdClaim, request.CommerceId.Value.ToString()));
            }

            var now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(_jwtSettings.ExpiryInMinutes),
                SigningCredentials = credentials
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }
}
