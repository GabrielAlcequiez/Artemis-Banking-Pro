using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ABP.WebApi.IntegrationTests;

public sealed class CreditCardsWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string Issuer = "ABP.Tests";
    private const string Audience = "ABP.Tests.Client";
    private const string SecretKey =
        "ABP_TESTS_JWT_SECRET_KEY_WITH_AT_LEAST_32_BYTES_2026";

    private readonly string _databaseName =
        $"CreditCardsHostTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=ABP_Tests;Integrated Security=True;TrustServerCertificate=True;",
                ["JwtSettings:Issuer"] = Issuer,
                ["JwtSettings:Audience"] = Audience,
                ["JwtSettings:SecretKey"] = SecretKey,
                ["Security:Cvc:SecretBase64"] = Convert.ToBase64String(
                    Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<IEmailService>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<IEmailService, NoOpEmailService>();
        });
    }

    public static string CreateJwt(
        string role,
        string? userId = null,
        Guid? commerceId = null)
    {
        userId ??= $"test-{role.ToLowerInvariant()}";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };

        if (commerceId.HasValue)
        {
            claims.Add(new Claim("CommerceId", commerceId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendAsync(EmailRequestDto emailRequestDto) =>
            Task.CompletedTask;
    }
}
