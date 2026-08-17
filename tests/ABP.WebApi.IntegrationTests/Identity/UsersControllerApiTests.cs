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
    public async Task POST_ApiAccountLogin_InvalidPayload_Returns400()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/login",
            new { userName = string.Empty, password = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiAccountLogin_InvalidPassword_Returns401()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/login",
            new { userName = "adminapi", password = "WrongPassw0rd!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiAccountLogin_DisallowedRole_Returns403()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/login",
            new
            {
                userName = "client",
                password = AuthWebApplicationFactory.DefaultPassword
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiAccountConfirm_InvalidToken_Returns400()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/confirm",
            new { token = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiAccountResetToken_InvalidUsername_Returns400()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/get-reset-token",
            new { userName = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiAccountResetPassword_MismatchedPasswords_Returns400()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/account/reset-password",
            new
            {
                userId = "user-1",
                token = "token",
                password = "NewPassw0rd!",
                confirmPassword = "DifferentPassw0rd!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var data = document.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.True(data.Length >= 4);
        Assert.All(data, item => Assert.NotEqual("Comercio", item.GetProperty("role").GetString()));
    }

    [Fact]
    public async Task GET_ApiUsers_Anonymous_Returns401()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_ApiUsers_NonAdminRole_Returns403()
    {
        var commerceId = await factory.GetUserIdAsync("commerceapi");
        using var client = CreateAuthenticatedClient(Roles.Commerce.ToString(), commerceId);

        using var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_ApiCommerceUsers_AdminRole_ReturnsOnlyCommerceUsers()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.GetAsync("/api/v1/users/commerce");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(data);
        Assert.All(data, item => Assert.Equal("Comercio", item.GetProperty("role").GetString()));
    }

    [Fact]
    public async Task GET_ApiUserDetail_AdminRole_Returns200()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.GetAsync($"/api/v1/users/{adminId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(adminId, document.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task POST_ApiUsers_InvalidPayload_Returns400()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new { firstName = "Incomplete" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiUsers_Anonymous_Returns401()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            ValidCreateUserRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ApiUsers_NonAdminRole_Returns403()
    {
        var commerceId = await factory.GetUserIdAsync("commerceapi");
        using var client = CreateAuthenticatedClient(Roles.Commerce.ToString(), commerceId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            ValidCreateUserRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    [Fact]
    public async Task GET_ApiUserDetail_MissingUser_Returns404()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_ApiUser_MissingUser_Returns404()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}",
            new
            {
                firstName = "Missing",
                lastName = "User",
                identification = "00999999998",
                email = "missing-user@test.com",
                userName = "missing-user",
                password = "",
                confirmPassword = "",
                additionalAmount = 0m
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PATCH_ApiUsersStatus_MissingUser_Returns404()
    {
        var adminId = await factory.GetUserIdAsync("adminapi");
        using var client = CreateAuthenticatedClient(Roles.Administrator.ToString(), adminId);

        using var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}/status",
            new { status = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static object ValidCreateUserRequest() => new
    {
        firstName = "New",
        lastName = "User",
        identification = "00999999997",
        email = $"new-{Guid.NewGuid():N}@test.com",
        userName = $"new-{Guid.NewGuid():N}"[..20],
        password = AuthWebApplicationFactory.DefaultPassword,
        confirmPassword = AuthWebApplicationFactory.DefaultPassword,
        role = "Cliente",
        initialAmount = 0m
    };
}
