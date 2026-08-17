using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ABP.WebApi.IntegrationTests;

public sealed class CommerceUserHostTests(
    CreditCardsWebApplicationFactory factory)
    : IClassFixture<CreditCardsWebApplicationFactory>
{
    [Fact]
    public async Task Create_commerce_user_requires_administrator_and_preserves_contract()
    {
        var accountService = new StubAccountService();
        using var webFactory = WithAccountService(accountService);
        using var anonymous = CreateClient(webFactory);
        using var commerceUser = CreateClient(webFactory, Roles.Commerce);
        using var administrator = CreateClient(webFactory, Roles.Administrator);
        var commerceId = Guid.NewGuid();
        var request = ValidRequest();

        var anonymousResponse = await anonymous.PostAsJsonAsync(
            $"/api/v1/Users/commerce/{commerceId}",
            request);
        var forbiddenResponse = await commerceUser.PostAsJsonAsync(
            $"/api/v1/Users/commerce/{commerceId}",
            request);
        var createdResponse = await administrator.PostAsJsonAsync(
            $"/api/v1/Users/commerce/{commerceId}",
            request);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal(commerceId, accountService.RegisteredCommerceId);
        Assert.Equal(request.UserName, accountService.RegisteredRequest!.UserName);

        using var json = await ReadJsonAsync(createdResponse);
        Assert.Equal(accountService.CreatedUserId, json.RootElement.GetProperty("id").GetString());
        Assert.Equal("Comercio", json.RootElement.GetProperty("role").GetString());
        Assert.Equal(commerceId, json.RootElement.GetProperty("commerceId").GetGuid());
        Assert.False(json.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Change_user_status_preserves_false_and_current_administrator()
    {
        var accountService = new StubAccountService();
        using var webFactory = WithAccountService(accountService);
        using var administrator = CreateClient(webFactory, Roles.Administrator, "admin-test");

        var response = await administrator.PatchAsJsonAsync(
            "/api/v1/Users/commerce-user/status",
            new { status = false });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("commerce-user", accountService.StatusUserId);
        Assert.False(accountService.RequestedStatus);
        Assert.Equal("admin-test", accountService.CurrentUserId);
    }

    private WebApplicationFactory<Program> WithAccountService(
        StubAccountService accountService) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAccountServiceForWebApi>();
            services.AddSingleton<IAccountServiceForWebApi>(accountService);
        }));

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> webFactory,
        Roles? role = null,
        string? userId = null)
    {
        var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (role.HasValue)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    CreditCardsWebApplicationFactory.CreateJwt(
                        role.Value.ToString(),
                        userId));
        }

        return client;
    }

    private static CreateCommerceUserRequestDto ValidRequest() => new()
    {
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana-commerce@example.test",
        UserName = "ana-commerce",
        Password = "Passw0rd!",
        ConfirmPassword = "Passw0rd!",
        InitialAmount = 100m
    };

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed class StubAccountService : IAccountServiceForWebApi
    {
        public string CreatedUserId { get; } = Guid.NewGuid().ToString();
        public Guid? RegisteredCommerceId { get; private set; }
        public CreateCommerceUserRequestDto? RegisteredRequest { get; private set; }
        public string? StatusUserId { get; private set; }
        public bool? RequestedStatus { get; private set; }
        public string? CurrentUserId { get; private set; }

        public Task<RegisterResponseDto> RegisterCommerceUserAsync(
            CreateCommerceUserRequestDto request,
            Guid commerceId,
            string? origin)
        {
            RegisteredCommerceId = commerceId;
            RegisteredRequest = request;
            return Task.FromResult(new RegisterResponseDto
            {
                Id = CreatedUserId,
                IsVerified = false
            });
        }

        public Task ChangeUserStatusAsync(
            string userId,
            ChangeUserStatusRequestDto request,
            string currentUserId)
        {
            StatusUserId = userId;
            RequestedStatus = request.Status;
            CurrentUserId = currentUserId;
            return Task.CompletedTask;
        }

        public Task<AuthenticationResponseDto> LoginAsync(LoginDto loginRequestDto) =>
            throw new NotSupportedException();

        public Task ConfirmAccountAsync(ConfirmAccountRequestDto request) =>
            throw new NotSupportedException();

        public Task GetResetTokenAsync(ForgotPasswordDto forgotPasswordDto) =>
            throw new NotSupportedException();

        public Task<RegisterResponseDto> RegisterUserAsync(CreateUserDto createUserDto, string? origin, bool isApi = false) =>
            throw new NotSupportedException();

        public Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string currentUserId, string? origin = null, bool isApi = false) =>
            throw new NotSupportedException();

        public Task<string> ConfirmAccountAsync(string userId, string token) =>
            throw new NotSupportedException();

        public Task<string> ConfirmAccountAsync(string token) =>
            throw new NotSupportedException();

        public Task<string?> ValidateResetTokenAsync(string userId, string token) =>
            throw new NotSupportedException();

        public Task<string> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? origin = null, bool isApi = false) =>
            throw new NotSupportedException();

        public Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto) =>
            throw new NotSupportedException();

        public Task<GetUserDto?> GetUserByIdAsync(string userId) =>
            throw new NotSupportedException();

        public Task<GetUserDto?> GetUserByUsernameAsync(string username) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GetUserDto>> GetAllUsersAsync() =>
            throw new NotSupportedException();

        public Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter) =>
            throw new NotSupportedException();

        public Task<UserResponseDto> ChangeUserStatusAsync(string userId, bool isActive, string currentUserId) =>
            throw new NotSupportedException();

        public Task<UserDetailDto?> GetUserDetailAsync(string userId) =>
            throw new NotSupportedException();
    }
}
