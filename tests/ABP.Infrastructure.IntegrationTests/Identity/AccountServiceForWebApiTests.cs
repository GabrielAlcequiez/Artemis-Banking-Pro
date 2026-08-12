using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Validation.Users;
using ABP.Application.Exceptions;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class AccountServiceForWebApiTests
{
    private const string Password = "Passw0rd!";

    private readonly FakeUserStore _userStore = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeJwtTokenService _jwtTokenService = new();

    private AccountServiceForWebApi CreateService()
    {
        var userManager = new UserManager<AppUser>(
            _userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);

        var signInManager = new SignInManager<AppUser>(
            userManager,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new UserClaimsPrincipalFactory<AppUser>(userManager, Options.Create(new IdentityOptions())),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<AppUser>>.Instance,
            new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())),
            new DefaultUserConfirmation<AppUser>());

        return new AccountServiceForWebApi(
            signInManager,
            new LoginValidator(),
            _jwtTokenService,
            null!, // mapper (no se usa en login)
            userManager,
            null!, // emailService
            null!, // createUserValidator
            null!, // editUserValidator
            null!, // resetPasswordValidator
            _userRepository,
            null!, // unitOfWork
            null!, // accountTokenService
            null!, // primaryAccountProvisioner
            null!, // savingsAccountRepository
            null!, // accountBalanceService
            null!, // accountLedger
            NullLogger<BaseAccountService>.Instance,
            null!, // commerceRepository
            null!, // createCommerceUserValidator
            new ConfirmAccountRequestValidator(),
            new ForgotPasswordDtoValidator(),
            new ChangeUserStatusRequestValidator(),
            new UserQueryFilterApiValidator());    }

    private void SeedUser(string userName, string role, bool isActive = true, bool emailConfirmed = true, Guid? commerceId = null)
    {
        var appUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@test.com",
            IsActive = isActive,
            EmailConfirmed = emailConfirmed
        };

        _userStore.SeedUser(appUser, Password, role);
        _userRepository.SeedUser(appUser.Id, new User(appUser.Id)
        {
            Name = userName,
            UserName = userName,
            Role = Enum.Parse<Roles>(role),
            IsActive = isActive,
            CommerceId = commerceId
        });
    }

    private static LoginDto ValidLogin(string userName = "admin") => new()
    {
        Username = userName,
        Password = Password
    };

    [Fact]
    public async Task LoginAsync_admin_active_user_returns_jwt_without_commerce_claim()
    {
        SeedUser("admin", Roles.Administrator.ToString());
        var service = CreateService();

        var response = await service.LoginAsync(ValidLogin());

        Assert.False(string.IsNullOrEmpty(response.Jwt));
        var request = Assert.Single(_jwtTokenService.Requests);
        Assert.Equal("admin", request.UserName);
        Assert.Equal(Roles.Administrator.ToString(), request.Role);
        Assert.Null(request.CommerceId);
    }

    [Fact]
    public async Task LoginAsync_commerce_active_user_returns_jwt_with_commerce_id()
    {
        var commerceId = Guid.NewGuid();
        SeedUser("commerce01", Roles.Commerce.ToString(), commerceId: commerceId);
        var service = CreateService();

        var response = await service.LoginAsync(ValidLogin("commerce01"));

        Assert.False(string.IsNullOrEmpty(response.Jwt));
        var request = Assert.Single(_jwtTokenService.Requests);
        Assert.Equal(Roles.Commerce.ToString(), request.Role);
        Assert.Equal(commerceId, request.CommerceId);
    }

    [Fact]
    public async Task LoginAsync_client_role_is_forbidden()
    {
        SeedUser("cliente01", Roles.Client.ToString());
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(ValidLogin("cliente01")));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Empty(_jwtTokenService.Requests);
    }

    [Fact]
    public async Task LoginAsync_cashier_role_is_forbidden()
    {
        SeedUser("cajero01", Roles.Cashier.ToString());
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(ValidLogin("cajero01")));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_inactive_user_is_rejected()
    {
        SeedUser("inactivo", Roles.Administrator.ToString(), isActive: false);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(ValidLogin("inactivo")));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Contains("inactiva", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_unconfirmed_email_is_rejected()
    {
        SeedUser("sinconfirmar", Roles.Administrator.ToString(), emailConfirmed: false);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(ValidLogin("sinconfirmar")));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_unknown_user_is_rejected()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(ValidLogin("noexiste")));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Empty(_jwtTokenService.Requests);
    }

    [Fact]
    public async Task LoginAsync_wrong_password_is_rejected()
    {
        SeedUser("admin", Roles.Administrator.ToString());
        var service = CreateService();

        var dto = ValidLogin();
        dto.Password = "WrongPassw0rd!";

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(dto));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Empty(_jwtTokenService.Requests);
    }

    [Fact]
    public async Task LoginAsync_missing_required_fields_throws_validation_exception()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.LoginAsync(new LoginDto()));
    }

    private sealed class FakeUserStore : IUserStore<AppUser>, IUserEmailStore<AppUser>, IUserPasswordStore<AppUser>, IUserRoleStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _usersById = new();
        private readonly Dictionary<string, AppUser> _usersByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _rolesByUser = new();

        public void SeedUser(AppUser user, string password, string role)
        {
            user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
            _usersById[user.Id] = user;
            _usersByName[user.NormalizedUserName ?? user.UserName ?? string.Empty] = user;
            _rolesByUser[user.Id] = [role];
        }

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById[user.Id] = user;
            _usersByName[user.NormalizedUserName ?? user.UserName ?? string.Empty] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById.Remove(user.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_usersByName.GetValueOrDefault(normalizedUserName));

        public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult<AppUser?>(null);

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id!);

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedEmail);

        public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        public Task AddToRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            var roles = _rolesByUser.GetValueOrDefault(user.Id) ?? [];
            roles.Add(roleName);
            _rolesByUser[user.Id] = roles;
            return Task.CompletedTask;
        }

        public Task RemoveFromRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (_rolesByUser.TryGetValue(user.Id, out var roles))
            {
                roles.Remove(roleName);
            }

            return Task.CompletedTask;
        }

        public Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult<IList<string>>(_rolesByUser.GetValueOrDefault(user.Id) ?? []);

        public Task<bool> IsInRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken) =>
            Task.FromResult((_rolesByUser.GetValueOrDefault(user.Id) ?? []).Contains(roleName));

        public Task<IList<AppUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) =>
            Task.FromResult<IList<AppUser>>([]);

        public void Dispose()
        {
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _usersById = new();

        public void SeedUser(string id, User user)
        {
            _usersById[id] = user;
        }

        public Task<User?> FindByIdentificationAsync(string identification) =>
            Task.FromResult<User?>(null);

        public Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<User>(new List<User>(), request.Page, request.PageSize, 0));

        public Task<PagedResult<User>> GetActiveClientsPagedAsync(PagedRequest request, string? identification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<User>(new List<User>(), request.Page, request.PageSize, 0));

        public Task<User?> GetActiveClientByIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.Values.FirstOrDefault(user =>
                user.Id == clientId && user.Role == Roles.Client && user.IsActive));

        public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.Values.Count(user =>
                user.Role == Roles.Client && user.IsActive));

        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.Values.Any(user => user.CommerceId == commerceId));

        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => new List<User>().AsQueryable();

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(id));

        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(new List<User>());

        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            _usersById[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task<User?> UpdateAsync(string id, User value, CancellationToken cancellationToken = default)
        {
            _usersById[id] = value;
            return Task.FromResult<User?>(value);
        }

        public Task<User?> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public List<TokenGenerationRequest> Requests { get; } = [];

        public string GenerateToken(TokenGenerationRequest request)
        {
            Requests.Add(request);
            return "fake-jwt";
        }
    }
}
