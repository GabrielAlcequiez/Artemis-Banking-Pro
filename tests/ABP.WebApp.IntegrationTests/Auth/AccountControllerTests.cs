using System.Reflection;
using System.Security.Claims;
using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Enums;
using ABP.Infrastructure.Identity;
using ABP.WebApp.Areas.Admin.Controllers;
using ABP.WebApp.Controllers;
using ABP.WebApp.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AdminHomeController = ABP.WebApp.Areas.Admin.Controllers.HomeController;

namespace ABP.WebApp.IntegrationTests.Auth;

public sealed class AccountControllerTests
{
    [Fact]
    public void ProtectedAdminRoute_requires_admin_role_and_login_redirect_path()
    {
        var authorize = typeof(AdminHomeController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(nameof(Roles.Administrator), authorize!.Roles);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ABP_AuthControllerTests;Trusted_Connection=True;"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServicesWebApp(configuration);
        using var provider = services.BuildServiceProvider();

        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal("/Account/Login", cookieOptions.LoginPath.Value);
    }

    [Theory]
    [InlineData(nameof(Roles.Administrator), "Admin")]
    [InlineData(nameof(Roles.Cashier), "Cashier")]
    [InlineData(nameof(Roles.Client), "Client")]
    public async Task Login_valid_credentials_redirects_to_area_home(
        string role,
        string area)
    {
        var service = new FakeAccountService
        {
            LoginResult = new LoginResponseDto
            {
                Roles = [role]
            }
        };
        var controller = new AccountController(service);

        var result = await controller.Login(new LoginViewModel
        {
            Username = "user",
            Password = "Passw0rd!"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal(area, redirect.RouteValues!["area"]);
    }

    [Fact]
    public async Task Login_invalid_credentials_renders_view_with_error()
    {
        var service = new FakeAccountService
        {
            LoginResult = new LoginResponseDto
            {
                HasError = true,
                Error = "Los datos de acceso son inválidos."
            }
        };
        var controller = new AccountController(service);
        var model = new LoginViewModel
        {
            Username = "user",
            Password = "wrong"
        };

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Equal("Los datos de acceso son inválidos.", model.Error);
    }

    [Fact]
    public void Login_get_when_already_authenticated_redirects_to_role_home()
    {
        var controller = new AccountController(new FakeAccountService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, nameof(Roles.Administrator))],
                    "Test"))
            }
        };

        var result = controller.Login((string?)null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Admin", redirect.RouteValues!["area"]);
    }

    [Theory]
    [InlineData(
        "El token de activación ya ha sido utilizado.",
        "Este enlace de activación ya fue utilizado.")]
    [InlineData(
        "El token de activación es inválido.",
        "El enlace de activación no es válido.")]
    public async Task Activate_maps_service_error_to_user_message(
        string serviceError,
        string expectedMessage)
    {
        var service = new FakeAccountService
        {
            ActivationError = serviceError
        };
        var controller = new AccountController(service);

        var result = await controller.Activate("user-1", "token");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AccountMessageViewModel>(view.Model);
        Assert.Equal(expectedMessage, model.Message);
    }

    private sealed class FakeAccountService : IAccountServiceForWebApp
    {
        public LoginResponseDto LoginResult { get; init; } = new();

        public string? ActivationError { get; init; }

        public Task<LoginResponseDto> LoginAsync(LoginDto loginRequestDto) =>
            Task.FromResult(LoginResult);

        public Task LogoutAsync() => Task.CompletedTask;

        public Task<UserUniquenessResponseDto> CheckRegistrationUniquenessAsync(
            string? identification,
            string? email,
            string? userName,
            string? excludeUserId = null) =>
            Task.FromResult(new UserUniquenessResponseDto());

        public Task<RegisterResponseDto> RegisterUserAsync(
            CreateUserDto createUserDto,
            string? origin,
            bool isApi = false) =>
            Task.FromResult(new RegisterResponseDto { Id = "user-1" });

        public Task<UserResponseDto> EditUserAsync(
            EditUserDto editUserDto,
            string currentUserId,
            string? origin = null,
            bool isApi = false) =>
            Task.FromResult(new UserResponseDto());

        public Task<string> ConfirmAccountAsync(string userId, string token) =>
            Task.FromResult(ActivationError ?? string.Empty);

        public Task<string> ConfirmAccountAsync(string token) =>
            Task.FromResult(ActivationError ?? string.Empty);

        public Task<string?> ValidateResetTokenAsync(string userId, string token) =>
            Task.FromResult<string?>(null);

        public Task<string> ForgotPasswordAsync(
            ForgotPasswordDto forgotPasswordDto,
            string? origin = null,
            bool isApi = false) =>
            Task.FromResult(string.Empty);

        public Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto) =>
            Task.FromResult(string.Empty);

        public Task<GetUserDto?> GetUserByIdAsync(string userId) =>
            Task.FromResult<GetUserDto?>(null);

        public Task<GetUserDto?> GetUserByUsernameAsync(string username) =>
            Task.FromResult<GetUserDto?>(null);

        public Task<IReadOnlyList<GetUserDto>> GetAllUsersAsync() =>
            Task.FromResult<IReadOnlyList<GetUserDto>>([]);

        public Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter) =>
            Task.FromResult(new PagedResultDto<GetUserDto>());

        public Task<UserResponseDto> ChangeUserStatusAsync(
            string userId,
            bool isActive,
            string currentUserId) =>
            Task.FromResult(new UserResponseDto());

        public Task<RegisterResponseDto> RegisterCommerceUserAsync(
            CreateCommerceUserRequestDto createCommerceUserRequest,
            Guid commerceId,
            string? origin) =>
            Task.FromResult(new RegisterResponseDto { Id = "commerce-user-1" });

        public Task<UserDetailDto?> GetUserDetailAsync(string userId) =>
            Task.FromResult<UserDetailDto?>(null);
    }
}
