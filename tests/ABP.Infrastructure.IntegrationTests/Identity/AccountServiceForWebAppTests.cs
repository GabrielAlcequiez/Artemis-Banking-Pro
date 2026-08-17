using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Validation.Users;
using ABP.Application.Mappings;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Identity.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class AccountServiceForWebAppTests
{
    private const string Password = "Passw0rd!";

    private readonly FakeUserStore _userStore = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddMaps(typeof(UserProfile).Assembly),
        NullLoggerFactory.Instance).CreateMapper();

    private AccountServiceForWebApp CreateService(StubSignInManager signInManager) => new(
        signInManager,
        new LoginValidator(),
        _mapper,
        CreateUserManager(),
        null!, // emailService (no se usa en login)
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
        null!); // createCommerceUserValidator

    private UserManager<AppUser> CreateUserManager() => new(
        _userStore,
        Options.Create(new IdentityOptions()),
        new PasswordHasher<AppUser>(),
        [],
        [],
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<AppUser>>.Instance);

    private StubSignInManager CreateSignInManager() => new(
        CreateUserManager(),
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        new UserClaimsPrincipalFactory<AppUser>(CreateUserManager(), Options.Create(new IdentityOptions())),
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<AppUser>>.Instance,
        new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())),
        new DefaultUserConfirmation<AppUser>());

    private void SeedUser(string userName, string role, bool isActive = true, bool emailConfirmed = true)
    {
        var appUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@test.com",
            NormalizedEmail = $"{userName}@test.com".ToUpperInvariant(),
            IsActive = isActive,
            EmailConfirmed = emailConfirmed
        };

        _userStore.SeedUser(appUser, Password, role);
        _userRepository.SeedUser(appUser.Id, new User(appUser.Id)
        {
            Name = userName,
            LastName = "Test",
            Email = appUser.Email,
            UserName = userName,
            Identification = $"00{appUser.Id[..9]}",
            Role = Enum.Parse<Roles>(role),
            IsActive = isActive
        });
    }

    private static LoginDto ValidLogin(string userName = "admin") => new()
    {
        Username = userName,
        Password = Password
    };

    [Fact]
    public async Task LoginAsync_ValidAdmin_ReturnsSuccessAndAdminRole()
    {
        SeedUser("admin", Roles.Administrator.ToString());
        var service = CreateService(CreateSignInManager());

        var response = await service.LoginAsync(ValidLogin());

        Assert.False(response.HasError);
        Assert.Equal("admin", response.Username);
        Assert.Contains(Roles.Administrator.ToString(), response.Roles!);
    }

    [Fact]
    public async Task LoginAsync_ValidCashier_ReturnsSuccessAndCashierRole()
    {
        SeedUser("cashier", Roles.Cashier.ToString());
        var service = CreateService(CreateSignInManager());

        var response = await service.LoginAsync(ValidLogin("cashier"));

        Assert.False(response.HasError);
        Assert.Contains(Roles.Cashier.ToString(), response.Roles!);
    }

    [Fact]
    public async Task LoginAsync_ValidClient_ReturnsSuccessAndClientRole()
    {
        SeedUser("client", Roles.Client.ToString());
        var service = CreateService(CreateSignInManager());

        var response = await service.LoginAsync(ValidLogin("client"));

        Assert.False(response.HasError);
        Assert.Contains(Roles.Client.ToString(), response.Roles!);
    }

    [Fact]
    public async Task LoginAsync_CommerceUser_IsRejectedWithNotAllowed()
    {
        SeedUser("commerce", Roles.Commerce.ToString());
        var service = CreateService(CreateSignInManager());

        var response = await service.LoginAsync(ValidLogin("commerce"));

        Assert.True(response.HasError);
        Assert.Equal("Este usuario no tiene permisos para acceder a la aplicación web.", response.Error);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_IsRejected()
    {
        SeedUser("inactivo", Roles.Administrator.ToString(), isActive: false);
        var service = CreateService(CreateSignInManager());

        var response = await service.LoginAsync(ValidLogin("inactivo"));

        Assert.True(response.HasError);
        Assert.Contains("Su cuenta se encuentra inactiva", response.Error);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsInvalidCredentials()
    {
        SeedUser("admin", Roles.Administrator.ToString());
        var signInManager = CreateSignInManager();
        signInManager.FailNextSignIn = true;
        var service = CreateService(signInManager);

        var dto = ValidLogin();
        dto.Password = "WrongPassw0rd!";

        var response = await service.LoginAsync(dto);

        Assert.True(response.HasError);
        Assert.Equal("Los datos de acceso son inválidos.", response.Error);
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
            Task.FromResult(user.Id);

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
            Task.FromResult(_usersById.Values.Count(user => user.Role == Roles.Client && user.IsActive));

        public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.Values.Count(user => user.Role == Roles.Client && !user.IsActive));

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

    private sealed class StubSignInManager : SignInManager<AppUser>
    {
        public StubSignInManager(
            UserManager<AppUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<AppUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<AppUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<AppUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public bool FailNextSignIn { get; set; }

        public override Task<SignInResult> PasswordSignInAsync(
            string userName,
            string password,
            bool isPersistent,
            bool lockoutOnFailure) =>
            Task.FromResult(FailNextSignIn ? SignInResult.Failed : SignInResult.Success);
    }
}