using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ABP.Application.Common.DTOs.Users;
using ABP.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ABP.WebApi.IntegrationTests;

public sealed class UsersControllerApiTests(
    AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.InitializeDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("adminapi", "Administrator")]
    [InlineData("commerceapi", "Commerce")]
    public async Task POST_ApiAccountLogin_AdminOrCommerce_Returns200WithJwt(
        string userName,
        string expectedRole)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/login",
            new { userName, password = AuthWebApplicationFactory.DefaultPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Jwt));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Jwt);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub);
        Assert.Contains(jwt.Claims, claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role") &&
            claim.Value == expectedRole);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task GET_ApiUsers_AdminRole_Returns200PagedUsers()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.GetAsync("/api/v1/users?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("page").GetInt32() >= 1);
        Assert.True(document.RootElement.GetProperty("pageSize").GetInt32() <= 20);
        Assert.True(document.RootElement.GetProperty("data").GetArrayLength() >= 4);
    }

    [Fact]
    public async Task POST_ApiUsers_DuplicateUser_Returns409Conflict()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);
        var request = new
        {
            firstName = "Duplicado",
            lastName = "Usuario",
            identification = "00999999999",
            email = "new-user@test.com",
            userName = "adminapi",
            password = AuthWebApplicationFactory.DefaultPassword,
            confirmPassword = AuthWebApplicationFactory.DefaultPassword,
            role = "Cliente",
            initialAmount = 0m
        };

        using var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PATCH_ApiUsersStatus_SelfChange_Returns403Forbidden()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{adminId}/status",
            new { status = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private HttpClient CreateAuthenticatedClient(string role, string userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AuthWebApplicationFactory.CreateJwt(role, userId));
        return client;
    }
}
