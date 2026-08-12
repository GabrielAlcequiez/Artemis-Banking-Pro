using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Common.Validation.Users;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Mappings;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Identity.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class BaseAccountServiceTests
{
    private readonly FakeUserStore _userStore = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePrimaryAccountProvisioner _primaryAccountProvisioner = new();
    private readonly FakeEmailService _emailService = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddMaps(typeof(UserProfile).Assembly),
        NullLoggerFactory.Instance).CreateMapper();

    private BaseAccountService CreateService()
    {
        var userManager = new UserManager<AppUser>(
            _userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);

        return new BaseAccountService(
            _mapper,
            userManager,
            _emailService,
            new RegisterUserValidator(),
            new UpdateUserValidator(),
            new ResetPasswordValidator(),
            _userRepository,
            new FakeUnitOfWork(),
            new FakeAccountTokenService(),
            _primaryAccountProvisioner,
            null!,
            null!,
            null!,
            NullLogger<BaseAccountService>.Instance,
            null!, // commerceRepository (no se usa en los tests actuales)
            new CreateCommerceUserRequestValidator());
    }

    private static CreateUserDto ValidClientDto(decimal? initialBalance = 100m) => new()
    {
        FirstName = "Juan",
        LastName = "Perez",
        Identification = "001-1234567-8",
        Email = "juan@test.com",
        UserName = "juanp",
        Password = "Passw0rd!",
        ConfirmPassword = "Passw0rd!",
        Role = "Cliente",
        InitialBalance = initialBalance
    };

    [Fact]
    public async Task RegisterUserAsync_with_client_role_provisions_principal_account()
    {
        var service = CreateService();

        var result = await service.RegisterUserAsync(ValidClientDto(100m), "https://localhost");

        Assert.False(result.HasError);
        Assert.False(result.IsVerified);
        Assert.NotEmpty(result.Id);

        var call = Assert.Single(_primaryAccountProvisioner.Calls);
        Assert.Equal(result.Id, call.OwnerUserId);
        Assert.Equal(100m, call.InitialBalance);

        Assert.Equal("CLIENT", Assert.Single(_userStore.GetRolesByUser(result.Id)));
        Assert.Equal(Roles.Client, Assert.Single(_userRepository.Added).Role);
    }

    [Fact]
    public async Task RegisterUserAsync_without_initial_balance_provisions_account_with_zero()
    {
        var service = CreateService();

        var result = await service.RegisterUserAsync(ValidClientDto(null), "https://localhost");

        Assert.False(result.HasError);
        Assert.Equal(0m, Assert.Single(_primaryAccountProvisioner.Calls).InitialBalance);
    }

    [Fact]
    public async Task RegisterUserAsync_with_administrator_role_does_not_provision_account()
    {
        var service = CreateService();
        var dto = ValidClientDto();
        dto.Role = "Administrador";

        var result = await service.RegisterUserAsync(dto, "https://localhost");

        Assert.False(result.HasError);
        Assert.Empty(_primaryAccountProvisioner.Calls);
        Assert.Equal("ADMINISTRATOR", Assert.Single(_userStore.GetRolesByUser(result.Id)));
    }

    [Fact]
    public async Task RegisterUserAsync_email_failure_does_not_rollback_user_or_account()
    {
        var service = CreateService();
        _emailService.ThrowOnSend = true;

        var result = await service.RegisterUserAsync(ValidClientDto(), "https://localhost");

        Assert.True(result.HasError);
        Assert.Equal("No fue posible enviar el correo de activación. Intente nuevamente más tarde.", result.Error);
        Assert.NotEmpty(result.Id);
        Assert.Single(_primaryAccountProvisioner.Calls);
        Assert.Single(_userRepository.Added);
    }

    [Fact]
    public async Task RegisterUserAsync_duplicate_identification_is_rejected()
    {
        _userRepository.DuplicateIdentification = "001-1234567-8";
        var service = CreateService();

        var result = await service.RegisterUserAsync(ValidClientDto(), "https://localhost");

        Assert.True(result.HasError);
        Assert.Equal("Ya existe un usuario registrado con este número de cédula.", result.Error);
        Assert.Empty(_primaryAccountProvisioner.Calls);
    }

    private sealed class FakeUserStore : IUserStore<AppUser>, IUserEmailStore<AppUser>, IUserPasswordStore<AppUser>, IUserRoleStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _usersById = new();
        private readonly Dictionary<string, AppUser> _usersByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AppUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _rolesByUser = new();

        public IReadOnlyList<string> GetRolesByUser(string userId) =>
            _rolesByUser.GetValueOrDefault(userId) ?? [];

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.Id ??= Guid.NewGuid().ToString();
            _usersById[user.Id] = user;
            _usersByName[user.NormalizedUserName ?? user.UserName ?? string.Empty] = user;
            var email = user.NormalizedEmail ?? user.Email;
            if (!string.IsNullOrEmpty(email))
            {
                _usersByEmail[email] = user;
            }

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
            Task.FromResult(_usersByEmail.GetValueOrDefault(normalizedEmail));

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
        public string? DuplicateIdentification { get; set; }

        public List<User> Added { get; } = [];

        public Task<User?> FindByIdentificationAsync(string identification) =>
            string.Equals(identification, DuplicateIdentification)
                ? Task.FromResult<User?>(new User("dup") { Identification = identification })
                : Task.FromResult<User?>(null);

        public Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<User>(new List<User>(), request.Page, request.PageSize, 0));

        public Task<PagedResult<User>> GetActiveClientsPagedAsync(PagedRequest request, string? identification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<User>(new List<User>(), request.Page, request.PageSize, 0));

        public Task<User?> GetActiveClientByIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => new List<User>().AsQueryable();

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(new List<User>());

        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            Added.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<User?> UpdateAsync(string id, User value, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(value);

        public Task<User?> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeAccountTokenService : IAccountTokenService
    {
        public Task<string> GenerateAsync(string userId, AccountTokenPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult("fake-token");

        public Task<AccountTokenValidationResult> ValidateAsync(string userId, string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AccountTokenValidationResult(AccountTokenValidationStatus.Valid));

        public Task<AccountTokenValidationResult> ValidateByTokenAsync(string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AccountTokenValidationResult(AccountTokenValidationStatus.Valid, UserId: "user"));

        public Task<bool> TryMarkAsUsedAsync(Guid accountTokenId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool ThrowOnSend { get; set; }

        public List<EmailRequestDto> Sent { get; } = [];

        public Task SendAsync(EmailRequestDto emailRequestDto)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("SMTP unavailable");
            }

            Sent.Add(emailRequestDto);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrimaryAccountProvisioner : IPrimaryAccountProvisioner
    {
        public List<(string OwnerUserId, decimal InitialBalance, string ActorUserId, string ActorRole)> Calls { get; } = [];

        public Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(
            string ownerUserId,
            decimal initialBalance,
            string actorUserId,
            string actorRole,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((ownerUserId, initialBalance, actorUserId, actorRole));
            return Task.FromResult(OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(Guid.NewGuid(), initialBalance, DateTimeOffset.UtcNow)));
        }
    }
}
