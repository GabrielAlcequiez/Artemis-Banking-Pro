using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Infrastructure.Identity;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ABP.WebApi.IntegrationTests;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "ABP.Auth.Tests";
    public const string Audience = "ABP.Auth.Tests.Client";
    public const string SecretKey =
        "ABP_AUTH_TESTS_JWT_SECRET_KEY_WITH_AT_LEAST_32_BYTES_2026";
    public const string DefaultPassword = "Passw0rd!";

    private readonly string _databaseName = $"ABP_AuthHostTests_{Guid.NewGuid():N}";
    private readonly string _identityDatabaseName =
        $"ABP_AuthHostTests_Identity_{Guid.NewGuid():N}";
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    TestDatabase.CreateConnectionString(_databaseName),
                ["SeedUsers:DefaultPassword"] = DefaultPassword,
                ["JwtSettings:Issuer"] = Issuer,
                ["JwtSettings:Audience"] = Audience,
                ["JwtSettings:SecretKey"] = SecretKey,
                ["JwtSettings:ExpiryInMinutes"] = "60",
                ["Security:Cvc:SecretBase64"] = Convert.ToBase64String(
                    Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, NoOpEmailService>();

            services.RemoveAll<IdentityContext>();
            services.RemoveAll<DbContextOptions<IdentityContext>>();
            services.AddDbContext<IdentityContext>(
                (_, options) => options.UseSqlServer(
                    TestDatabase.CreateConnectionString(_identityDatabaseName),
                    sql => sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName)),
                contextLifetime: ServiceLifetime.Scoped,
                optionsLifetime: ServiceLifetime.Scoped);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var appContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var identityContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();
            await appContext.Database.EnsureDeletedAsync();
            await identityContext.Database.EnsureDeletedAsync();
            await appContext.Database.EnsureCreatedAsync();
            await identityContext.Database.EnsureCreatedAsync();
            await scope.ServiceProvider.RunSeedsAsync();
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<string> GetUserIdAsync(string userName)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var user = await userManager.FindByNameAsync(userName);
        return AssertUserId(userName, user?.Id);
    }

    public static string CreateJwt(string role, string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };
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

    private static string AssertUserId(string userName, string? userId) =>
        userId ?? throw new InvalidOperationException($"Seed user '{userName}' was not found.");

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendAsync(EmailRequestDto emailRequestDto) =>
            Task.CompletedTask;
    }
}
