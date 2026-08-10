using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ABP.Application.Common.DTOs.Users;
using ABP.Domain.Settings;
using ABP.Infrastructure.Identity.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class JwtTokenServiceTests
{
    private const string SecretKey = "test-secret-key-0123456789-abcdefghijklmn";
    private const string DifferentSecretKey = "different-secret-key-0123456789-abcdefgh";
    private const string Issuer = "ArtemisTestIssuer";
    private const string Audience = "ArtemisTestAudience";

    private static JwtTokenService CreateService(int expiryInMinutes = 60) =>
        new(Options.Create(new JwtSettings
        {
            SecretKey = SecretKey,
            Issuer = Issuer,
            Audience = Audience,
            ExpiryInMinutes = expiryInMinutes
        }));

    private static TokenValidationParameters CreateValidationParameters(string secretKey = SecretKey) =>
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

    private static ClaimsPrincipal Validate(string token, string secretKey = SecretKey)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, CreateValidationParameters(secretKey), out _);
    }

    [Fact]
    public void GenerateToken_contains_expected_claims()
    {
        var service = CreateService();

        var token = service.GenerateToken(new TokenGenerationRequest
        {
            UserId = "user-123",
            UserName = "admin",
            Role = "Administrator"
        });

        var principal = Validate(token);

        Assert.Equal("user-123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("admin", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Administrator", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Null(principal.FindFirst("CommerceId"));
    }

    [Fact]
    public void GenerateToken_includes_commerce_id_claim_only_when_present()
    {
        var commerceId = Guid.NewGuid();
        var service = CreateService();

        var token = service.GenerateToken(new TokenGenerationRequest
        {
            UserId = "user-456",
            UserName = "commerce01",
            Role = "Commerce",
            CommerceId = commerceId
        });

        var principal = Validate(token);

        Assert.Equal(commerceId.ToString(), principal.FindFirst("CommerceId")?.Value);
    }

    [Fact]
    public void GenerateToken_expires_after_configured_minutes()
    {
        const int expiryMinutes = 90;
        var service = CreateService(expiryMinutes);

        var token = service.GenerateToken(new TokenGenerationRequest
        {
            UserId = "user-789",
            UserName = "admin",
            Role = "Administrator"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var lifetime = jwt.ValidTo - jwt.ValidFrom;
        Assert.True(lifetime > TimeSpan.FromMinutes(expiryMinutes - 1));
        Assert.True(lifetime <= TimeSpan.FromMinutes(expiryMinutes + 1));
    }

    [Fact]
    public void GenerateToken_roundtrip_validates_with_same_key()
    {
        var service = CreateService();

        var token = service.GenerateToken(new TokenGenerationRequest
        {
            UserId = "user-000",
            UserName = "admin",
            Role = "Administrator"
        });

        var principal = Validate(token);

        Assert.True(principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public void GenerateToken_is_rejected_with_different_key()
    {
        var service = CreateService();

        var token = service.GenerateToken(new TokenGenerationRequest
        {
            UserId = "user-000",
            UserName = "admin",
            Role = "Administrator"
        });

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, DifferentSecretKey));
    }
}
