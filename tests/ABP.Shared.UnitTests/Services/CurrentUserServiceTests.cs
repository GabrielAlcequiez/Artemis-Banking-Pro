using System.Security.Claims;
using ABP.Application.Common.Interfaces.Services;
using ABP.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Shared.UnitTests.Services;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void Shared_registration_exposes_current_user_service()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSharedServices(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<CurrentUserService>(
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>());
    }

    [Fact]
    public void Service_reads_authenticated_user_context_from_claims()
    {
        var commerceId = Guid.NewGuid();
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "Ada"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("commerce_id", commerceId.ToString()));
        var service = CreateService(principal);

        Assert.True(service.IsAuthenticated);
        Assert.Equal("admin-1", service.UserId);
        Assert.Equal("Ada", service.UserName);
        Assert.Equal(commerceId, service.CommerceId);
        Assert.Equal(["Administrator"], service.Roles);
        Assert.True(service.IsInRole("Administrator"));
        Assert.False(service.IsInRole("Client"));
    }

    [Fact]
    public void Service_uses_sub_claim_as_user_id_fallback()
    {
        var service = CreateService(CreatePrincipal(new Claim("sub", "commerce-user")));

        Assert.Equal("commerce-user", service.UserId);
    }

    [Fact]
    public void Service_returns_empty_context_when_there_is_no_http_request()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.UserName);
        Assert.Null(service.CommerceId);
        Assert.Empty(service.Roles);
        Assert.False(service.IsInRole("Administrator"));
    }

    private static CurrentUserService CreateService(ClaimsPrincipal principal)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        return new CurrentUserService(accessor);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));
}
