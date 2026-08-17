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
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;
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
    private readonly FakeAccountTokenService _accountTokenService = new();
    private readonly FakeCommerceRepository _commerceRepository = new();
    private readonly FakeSavingsAccountRepository _savingsAccountRepository = new();
    private readonly FakeAccountBalanceService _accountBalanceService = new();
    private readonly FakeAccountLedger _accountLedger = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddMaps(typeof(UserProfile).Assembly),
        NullLoggerFactory.Instance).CreateMapper();

    private BaseAccountService CreateService()
    {
        var identityOptions = new IdentityOptions();
        identityOptions.Tokens.EmailConfirmationTokenProvider = TestTokenProvider.Name;
        identityOptions.Tokens.PasswordResetTokenProvider = TestTokenProvider.Name;

        var userManager = new UserManager<AppUser>(
            _userStore,
            Options.Create(identityOptions),
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);
        userManager.RegisterTokenProvider(TestTokenProvider.Name, new TestTokenProvider());

        return new BaseAccountService(
            _mapper,
            userManager,
            _emailService,
            new RegisterUserValidator(),
            new UpdateUserValidator(),
            new ResetPasswordValidator(),
            _userRepository,
            new FakeUnitOfWork(),
            _accountTokenService,
            _primaryAccountProvisioner,
            _savingsAccountRepository,
            _accountBalanceService,
            _accountLedger,
            NullLogger<BaseAccountService>.Instance,
            _commerceRepository,
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

    private static CreateCommerceUserRequestDto ValidCommerceUserDto() => new()
    {
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana-commerce@test.com",
        UserName = "ana-commerce",
        Password = "Passw0rd!",
        ConfirmPassword = "Passw0rd!",
        InitialAmount = 250m
    };

    private static EditUserDto ValidEditDto(string userId, decimal? additionalAmount = null) => new()
    {
        Id = userId,
        FirstName = "Ana actualizada",
        LastName = "Pérez actualizada",
        Identification = "00112345678",
        Email = $"updated-{userId}@test.com",
        UserName = $"updated-{userId[..8]}",
        AdditionalAmount = additionalAmount
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

    [Fact]
    public async Task RegisterUserAsync_duplicate_username_returns_conflict()
    {
        var existingUser = new AppUser
        {
            Id = "existing-username",
            UserName = "juanp",
            Email = "other@test.com",
            IsActive = true,
            EmailConfirmed = true
        };
        existingUser.PasswordHash = new PasswordHasher<AppUser>().HashPassword(existingUser, "Passw0rd!");
        _userStore.SeedUser(existingUser);
        await _userStore.AddToRoleAsync(existingUser, Roles.Administrator.ToString(), CancellationToken.None);
        var service = CreateService();

        var result = await service.RegisterUserAsync(ValidClientDto(), "https://localhost");

        Assert.True(result.HasError);
        Assert.True(result.IsConflict);
        Assert.Equal("Ya existe un usuario registrado con este nombre de usuario.", result.Error);
        Assert.Empty(_primaryAccountProvisioner.Calls);
    }

    [Fact]
    public async Task RegisterUserAsync_duplicate_email_returns_conflict()
    {
        var existingUser = new AppUser
        {
            Id = "existing-email",
            UserName = "other-user",
            Email = "juan@test.com",
            IsActive = true,
            EmailConfirmed = true
        };
        existingUser.PasswordHash = new PasswordHasher<AppUser>().HashPassword(existingUser, "Passw0rd!");
        _userStore.SeedUser(existingUser);
        await _userStore.AddToRoleAsync(existingUser, Roles.Administrator.ToString(), CancellationToken.None);
        var service = CreateService();

        var result = await service.RegisterUserAsync(ValidClientDto(), "https://localhost");

        Assert.True(result.HasError);
        Assert.True(result.IsConflict);
        Assert.Equal("Ya existe un usuario registrado con este correo electrónico.", result.Error);
        Assert.Empty(_primaryAccountProvisioner.Calls);
    }

    [Fact]
    public async Task RegisterCommerceUserAsync_creates_inactive_user_role_account_and_token()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        var service = CreateService();

        var result = await service.RegisterCommerceUserAsync(
            ValidCommerceUserDto(),
            commerce.Id,
            origin: null);

        Assert.False(result.HasError);
        Assert.NotEmpty(result.Id);
        Assert.False(result.IsVerified);
        Assert.Equal("COMMERCE", Assert.Single(_userStore.GetRolesByUser(result.Id)));

        var domainUser = Assert.Single(_userRepository.Added);
        Assert.Equal(Roles.Commerce, domainUser.Role);
        Assert.False(domainUser.IsActive);
        Assert.Equal(commerce.Id, domainUser.CommerceId);

        var accountCall = Assert.Single(_primaryAccountProvisioner.Calls);
        Assert.Equal(result.Id, accountCall.OwnerUserId);
        Assert.Equal(250m, accountCall.InitialBalance);
        Assert.Single(_accountTokenService.Generated);
    }

    [Fact]
    public async Task RegisterCommerceUserAsync_missing_commerce_is_not_found_without_writes()
    {
        var service = CreateService();

        var result = await service.RegisterCommerceUserAsync(
            ValidCommerceUserDto(),
            Guid.NewGuid(),
            origin: null);

        Assert.True(result.HasError);
        Assert.True(result.IsNotFound);
        Assert.Empty(_userRepository.Added);
        Assert.Empty(_primaryAccountProvisioner.Calls);
    }

    [Fact]
    public async Task RegisterCommerceUserAsync_existing_association_is_conflict_without_writes()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        _userRepository.Seed(new User("existing-commerce-user")
        {
            Name = "Existente",
            LastName = "Comercio",
            Identification = "00100000001",
            Email = "existing@test.com",
            UserName = "existing-commerce",
            Role = Roles.Commerce,
            IsActive = false,
            CommerceId = commerce.Id
        });
        var service = CreateService();

        var result = await service.RegisterCommerceUserAsync(
            ValidCommerceUserDto(),
            commerce.Id,
            origin: null);

        Assert.True(result.HasError);
        Assert.True(result.IsConflict);
        Assert.Empty(_userRepository.Added);
        Assert.Empty(_primaryAccountProvisioner.Calls);
    }

    [Fact]
    public async Task RegisterCommerceUserAsync_role_assignment_failure_stops_domain_writes()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        _userStore.FailNextUpdate = true;
        var service = CreateService();

        var result = await service.RegisterCommerceUserAsync(
            ValidCommerceUserDto(),
            commerce.Id,
            origin: null);

        Assert.True(result.HasError);
        Assert.Empty(result.Id);
        Assert.Empty(_userRepository.Added);
        Assert.Empty(_primaryAccountProvisioner.Calls);
        Assert.Empty(_accountTokenService.Generated);
    }

    [Fact]
    public async Task RegisterCommerceUserAsync_email_failure_keeps_created_response()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        _emailService.ThrowOnSend = true;
        var service = CreateService();

        var result = await service.RegisterCommerceUserAsync(
            ValidCommerceUserDto(),
            commerce.Id,
            origin: null);

        Assert.False(result.HasError);
        Assert.NotEmpty(result.Id);
        Assert.Single(_userRepository.Added);
        Assert.Single(_primaryAccountProvisioner.Calls);
    }

    [Fact]
    public async Task EditUserAsync_empty_password_preserves_existing_password_hash()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: true);
        var originalPasswordHash = _userStore.GetUser(userId)!.PasswordHash;
        var service = CreateService();

        var result = await service.EditUserAsync(
            ValidEditDto(userId),
            currentUserId: "admin-id");

        Assert.False(result.HasError);
        Assert.Equal(originalPasswordHash, _userStore.GetUser(userId)!.PasswordHash);
    }

    [Fact]
    public async Task EditUserAsync_additional_amount_zero_creates_no_ledger_movement()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: true);
        _savingsAccountRepository.SeedPrincipal(userId);
        var service = CreateService();

        var result = await service.EditUserAsync(
            ValidEditDto(userId, additionalAmount: 0m),
            currentUserId: "admin-id");

        Assert.False(result.HasError);
        Assert.Empty(_accountBalanceService.Credits);
        Assert.Empty(_accountLedger.ApprovedEntries);
    }

    [Fact]
    public async Task EditUserAsync_additional_amount_positive_credits_account_and_records_ledger()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: true);
        var principal = _savingsAccountRepository.SeedPrincipal(userId);
        var service = CreateService();

        var result = await service.EditUserAsync(
            ValidEditDto(userId, additionalAmount: 75m),
            currentUserId: "admin-id");

        Assert.False(result.HasError);
        var credit = Assert.Single(_accountBalanceService.Credits);
        Assert.Equal(principal.Id, credit.AccountId);
        Assert.Equal(75m, credit.Amount);

        var entry = Assert.Single(_accountLedger.ApprovedEntries);
        Assert.Equal(principal.Id, entry.AccountId);
        Assert.Equal(75m, entry.Amount);
        Assert.Equal(TransactionDirection.Credit, entry.Direction);
        Assert.Equal(FinancialOperationType.AdministrativeCredit, entry.OperationType);
    }

    [Fact]
    public async Task EditUserAsync_self_edit_is_forbidden()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: true);
        var service = CreateService();

        var result = await service.EditUserAsync(
            ValidEditDto(userId),
            currentUserId: userId);

        Assert.True(result.HasError);
        Assert.True(result.IsForbidden);
        Assert.Equal("No puede editar su propia cuenta desde este módulo.", result.Error);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_self_status_change_is_forbidden()
    {
        var userId = Guid.NewGuid().ToString();
        var service = CreateService();

        var result = await service.ChangeUserStatusAsync(userId, false, currentUserId: userId);

        Assert.True(result.HasError);
        Assert.True(result.IsForbidden);
        Assert.Equal("No puede modificar el estado de su propia cuenta.", result.Error);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_cannot_manually_reactivate_inactive_commerce_user()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        var userId = SeedCommerceUser(commerce.Id, isActive: false);
        var service = CreateService();

        var result = await service.ChangeUserStatusAsync(userId, true, "admin-id");

        Assert.True(result.HasError);
        Assert.Contains("confirmación o restablecimiento", result.Error);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_active_commerce_user_is_idempotent()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        var userId = SeedCommerceUser(commerce.Id, isActive: true);
        var service = CreateService();

        var result = await service.ChangeUserStatusAsync(userId, true, "admin-id");

        Assert.False(result.HasError);
        Assert.True(_userStore.GetUser(userId)!.IsActive);
        Assert.True((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_inactive_commerce_does_not_consume_token_or_activate_user()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Inactive);
        var userId = SeedCommerceUser(commerce.Id, isActive: false);
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Equal("El usuario de comercio no puede activarse mientras el comercio esté inactivo.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_active_commerce_consumes_token_and_activates_both_users()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        var userId = SeedCommerceUser(commerce.Id, isActive: false);
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Empty(error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.True(_userStore.GetUser(userId)!.IsActive);
        Assert.True(_userStore.GetUser(userId)!.EmailConfirmed);
        Assert.True((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_inactive_commerce_does_not_consume_token_or_activate_user()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Inactive);
        var userId = SeedCommerceUser(commerce.Id, isActive: false);
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.Equal("El usuario de comercio no puede activarse mientras el comercio esté inactivo.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_active_commerce_consumes_token_and_activates_both_users()
    {
        var commerce = _commerceRepository.Seed(CommerceStatus.Active);
        var userId = SeedCommerceUser(commerce.Id, isActive: false);
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.Empty(error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.True(_userStore.GetUser(userId)!.IsActive);
        Assert.True(_userStore.GetUser(userId)!.EmailConfirmed);
        Assert.True((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_lost_token_race_does_not_activate_user()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        _accountTokenService.MarkAsUsedResult = false;
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Equal("El token de activación ya ha sido utilizado.", error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_lost_token_race_does_not_change_password_or_activate_user()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        var originalPasswordHash = _userStore.GetUser(userId)!.PasswordHash;
        _accountTokenService.MarkAsUsedResult = false;
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.Equal("Este enlace de restablecimiento ya fue utilizado.", error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.Equal(originalPasswordHash, _userStore.GetUser(userId)!.PasswordHash);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_valid_token_activates_account_and_marks_token_used()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Empty(error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.True(_userStore.GetUser(userId)!.IsActive);
        Assert.True(_userStore.GetUser(userId)!.EmailConfirmed);
        Assert.True((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_used_token_is_rejected()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        _accountTokenService.ValidationStatus = AccountTokenValidationStatus.Used;
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Equal("El token de activación ya ha sido utilizado.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ConfirmAccountAsync_missing_token_is_rejected()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        _accountTokenService.ValidationStatus = AccountTokenValidationStatus.NotFound;
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Equal("El token de activación no fue encontrado.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
    }

    [Fact]
    public async Task ConfirmAccountAsync_invalid_token_is_rejected()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        _accountTokenService.ValidationStatus = AccountTokenValidationStatus.Invalid;
        var service = CreateService();

        var error = await service.ConfirmAccountAsync(userId, "activation-token");

        Assert.Equal("El token de activación es inválido.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
    }

    [Fact]
    public async Task ForgotPasswordAsync_valid_user_deactivates_and_generates_token()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: true);
        var appUser = _userStore.GetUser(userId)!;
        await _userStore.AddToRoleAsync(appUser, Roles.Client.ToString(), CancellationToken.None);
        var service = CreateService();

        var error = await service.ForgotPasswordAsync(new ForgotPasswordDto { Username = appUser.UserName! });

        Assert.Empty(error);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False(_userStore.GetUser(userId)!.EmailConfirmed);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
        Assert.Contains(_accountTokenService.Generated, generated => generated.Purpose == AccountTokenPurpose.PasswordReset);
        Assert.Single(_emailService.Sent);
    }

    [Fact]
    public async Task ResetPasswordAsync_expired_token_keeps_account_inactive()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        var originalPasswordHash = _userStore.GetUser(userId)!.PasswordHash;
        _accountTokenService.ValidationStatus = AccountTokenValidationStatus.Expired;
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.Equal("El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
        Assert.Equal(originalPasswordHash, _userStore.GetUser(userId)!.PasswordHash);
        Assert.False(_userStore.GetUser(userId)!.IsActive);
        Assert.False((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_mismatched_passwords_is_rejected()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "Different!"
        });

        Assert.Contains("deben coincidir", error);
        Assert.Equal(0, _accountTokenService.MarkAsUsedCalls);
    }

    [Fact]
    public async Task ResetPasswordAsync_valid_token_reactivates_user_and_updates_password()
    {
        var userId = SeedDomainAndIdentityUser(Roles.Client, commerceId: null, isActive: false);
        var originalPasswordHash = _userStore.GetUser(userId)!.PasswordHash;
        var service = CreateService();

        var error = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = userId,
            Token = "reset-token",
            Password = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.Empty(error);
        Assert.Equal(1, _accountTokenService.MarkAsUsedCalls);
        Assert.NotEqual(originalPasswordHash, _userStore.GetUser(userId)!.PasswordHash);
        Assert.True(_userStore.GetUser(userId)!.IsActive);
        Assert.True(_userStore.GetUser(userId)!.EmailConfirmed);
        Assert.True((await _userRepository.GetByIdAsync(userId))!.IsActive);
    }

    private string SeedCommerceUser(Guid commerceId, bool isActive) =>
        SeedDomainAndIdentityUser(Roles.Commerce, commerceId, isActive);

    private string SeedDomainAndIdentityUser(Roles role, Guid? commerceId, bool isActive)
    {
        var userId = Guid.NewGuid().ToString();
        _userStore.SeedUser(new AppUser
        {
            Id = userId,
            UserName = $"user-{userId[..8]}",
            Email = $"{userId}@test.com",
            EmailConfirmed = false,
            IsActive = isActive,
            PasswordHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), "Passw0rd!")
        });
        _userRepository.Seed(new User(userId)
        {
            Name = "Ana",
            LastName = "Pérez",
            Identification = userId[..11],
            Email = $"{userId}@test.com",
            UserName = $"user-{userId[..8]}",
            Role = role,
            IsActive = isActive,
            CommerceId = commerceId
        });

        return userId;
    }

    private sealed class FakeUserStore : IUserStore<AppUser>, IUserEmailStore<AppUser>, IUserPasswordStore<AppUser>, IUserRoleStore<AppUser>, IUserSecurityStampStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _usersById = new();
        private readonly Dictionary<string, AppUser> _usersByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AppUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _rolesByUser = new();

        public bool FailNextUpdate { get; set; }

        public IReadOnlyList<string> GetRolesByUser(string userId) =>
            _rolesByUser.GetValueOrDefault(userId) ?? [];

        public AppUser? GetUser(string userId) =>
            _usersById.GetValueOrDefault(userId);

        public void SeedUser(AppUser user)
        {
            user.NormalizedUserName ??= user.UserName?.ToUpperInvariant();
            user.NormalizedEmail ??= user.Email?.ToUpperInvariant();
            _usersById[user.Id] = user;
            _usersByName[user.NormalizedUserName ?? user.UserName ?? string.Empty] = user;
            if (!string.IsNullOrEmpty(user.NormalizedEmail))
            {
                _usersByEmail[user.NormalizedEmail] = user;
            }
        }

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
            if (FailNextUpdate)
            {
                FailNextUpdate = false;
                return Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Description = "No fue posible persistir el rol." }));
            }

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

        public Task<string?> GetSecurityStampAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.SecurityStamp);

        public Task SetSecurityStampAsync(AppUser user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _usersById = new();

        public string? DuplicateIdentification { get; set; }

        public List<User> Added { get; } = [];

        public void Seed(User user) => _usersById[user.Id] = user;

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

        public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.Values.Any(user => user.CommerceId == commerceId));

        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => new List<User>().AsQueryable();

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(id));

        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(new List<User>());

        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            Added.Add(entity);
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

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeAccountTokenService : IAccountTokenService
    {
        public List<(string UserId, AccountTokenPurpose Purpose)> Generated { get; } = [];

        public AccountTokenValidationStatus ValidationStatus { get; set; } = AccountTokenValidationStatus.Valid;

        public bool MarkAsUsedResult { get; set; } = true;

        public int MarkAsUsedCalls { get; private set; }

        public Task<string> GenerateAsync(string userId, AccountTokenPurpose purpose, CancellationToken cancellationToken = default)
        {
            Generated.Add((userId, purpose));
            return Task.FromResult("fake-token");
        }

        public Task<AccountTokenValidationResult> ValidateAsync(string userId, string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AccountTokenValidationResult(
                ValidationStatus,
                Guid.NewGuid(),
                userId));

        public Task<AccountTokenValidationResult> ValidateByTokenAsync(string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AccountTokenValidationResult(
                ValidationStatus,
                Guid.NewGuid(),
                "user"));

        public Task<bool> TryMarkAsUsedAsync(Guid accountTokenId, CancellationToken cancellationToken = default)
        {
            MarkAsUsedCalls++;
            return Task.FromResult(MarkAsUsedResult);
        }
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

    private sealed class FakeSavingsAccountRepository : ISavingsAccountRepository
    {
        private readonly Dictionary<Guid, SavingsAccount> _accounts = new();

        public SavingsAccount SeedPrincipal(string ownerUserId)
        {
            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = ownerUserId,
                AccountNumber = $"{Random.Shared.Next(100_000_000, 999_999_999)}",
                Balance = 0m,
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active
            };
            _accounts[account.Id] = account;
            return account;
        }

        public Task<SavingsAccount?> GetByAccountNumberAsync(
            string accountNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(account =>
                account.AccountNumber == accountNumber));

        public Task<SavingsAccount?> GetPrincipalAccountAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(account =>
                account.OwnerUserId == ownerUserId &&
                account.Type == SavingsAccountType.Principal &&
                account.Status == SavingsAccountStatus.Active));

        public Task<bool> AccountNumberExistsAsync(
            string accountNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.Any(account =>
                account.AccountNumber == accountNumber));

        public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<SavingsAccount>>(_accounts.Values
                .Where(account => account.OwnerUserId == ownerUserId && account.Status == SavingsAccountStatus.Active)
                .ToArray());

        public Task<PagedResult<SavingsAccount>> GetPagedAsync(
            PagedRequest request,
            string? ownerIdentification = null,
            SavingsAccountStatus? status = null,
            SavingsAccountType? type = null,
            CancellationToken cancellationToken = default)
        {
            var accounts = _accounts.Values
                .Where(account => !status.HasValue || account.Status == status)
                .Where(account => !type.HasValue || account.Type == type)
                .ToArray();
            return Task.FromResult(new PagedResult<SavingsAccount>(
                accounts,
                request.Page,
                request.PageSize,
                accounts.Length));
        }

        public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) =>
            _accounts.Values.AsQueryable();

        public Task<SavingsAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.GetValueOrDefault(id));

        public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(
            bool trackChanges = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavingsAccount>>(_accounts.Values.ToArray());

        public Task<SavingsAccount> AddAsync(
            SavingsAccount entity,
            CancellationToken cancellationToken = default)
        {
            _accounts[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task<SavingsAccount?> UpdateAsync(
            Guid id,
            SavingsAccount value,
            CancellationToken cancellationToken = default)
        {
            _accounts[id] = value;
            return Task.FromResult<SavingsAccount?>(value);
        }

        public Task<SavingsAccount?> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _accounts.Remove(id, out var account);
            return Task.FromResult(account);
        }
    }

    private sealed class FakeAccountBalanceService : IAccountBalanceService
    {
        public List<(Guid AccountId, decimal Amount)> Credits { get; } = [];

        public Task<OperationResult> CreditAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            Credits.Add((accountId, amount));
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> DebitAsync(
            Guid accountId,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());
    }

    private sealed class FakeAccountLedger : IAccountLedger
    {
        public sealed record ApprovedLedgerEntry(
            Guid OperationId,
            Guid AccountId,
            decimal Amount,
            TransactionDirection Direction,
            FinancialOperationType OperationType,
            string? Origin,
            string? Beneficiary,
            string? ActorUserId,
            string? ActorRole);

        public List<ApprovedLedgerEntry> ApprovedEntries { get; } = [];

        public Task RecordApprovedAsync(
            Guid operationId,
            Guid accountId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string? origin,
            string? beneficiary,
            string? actorUserId,
            string? actorRole,
            CancellationToken cancellationToken = default)
        {
            ApprovedEntries.Add(new ApprovedLedgerEntry(
                operationId,
                accountId,
                amount,
                direction,
                operationType,
                origin,
                beneficiary,
                actorUserId,
                actorRole));
            return Task.CompletedTask;
        }

        public Task RecordRejectedAsync(
            Guid accountId,
            Guid operationId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string rejectionReason,
            string? actorUserId,
            string? actorRole,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeCommerceRepository : ICommerceRepository
    {
        private readonly Dictionary<Guid, Commerce> _commerces = new();

        public Commerce Seed(CommerceStatus status)
        {
            var commerce = new Commerce
            {
                Name = "Tienda de prueba",
                Email = $"{Guid.NewGuid():N}@test.com",
                PhoneNumber = "8095551234",
                Rnc = Guid.NewGuid().ToString("N")[..9],
                Status = status
            };
            typeof(BaseEntity<Guid>)
                .GetProperty(nameof(BaseEntity<Guid>.Id))!
                .SetValue(commerce, Guid.NewGuid());
            _commerces[commerce.Id] = commerce;
            return commerce;
        }

        public Task<bool> EmailExistsAsync(string email, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> RncExistsAsync(string rnc, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<PagedResult<CommerceSummaryReadModel>> SearchAsync(int page, int pageSize, CommerceStatusFilter? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<CommerceSummaryReadModel>([], page, pageSize, 0));

        public Task<CommerceDetailReadModel?> GetDetailsAsync(Guid commerceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CommerceDetailReadModel?>(null);

        public Task<Commerce?> GetForUpdateAsync(Guid commerceId, CancellationToken cancellationToken = default) =>
            GetByIdAsync(commerceId, cancellationToken);

        public IQueryable<Commerce> GetAllQueryable(bool trackChanges = false) =>
            _commerces.Values.AsQueryable();

        public Task<Commerce?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_commerces.GetValueOrDefault(id));

        public Task<IReadOnlyList<Commerce>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Commerce>>(_commerces.Values.ToList());

        public Task<Commerce> AddAsync(Commerce entity, CancellationToken cancellationToken = default)
        {
            _commerces[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task<Commerce?> UpdateAsync(Guid id, Commerce value, CancellationToken cancellationToken = default)
        {
            _commerces[id] = value;
            return Task.FromResult<Commerce?>(value);
        }

        public Task<Commerce?> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _commerces.Remove(id, out var commerce);
            return Task.FromResult(commerce);
        }
    }

    private sealed class TestTokenProvider : IUserTwoFactorTokenProvider<AppUser>
    {
        public const string Name = "AlwaysValid";

        public Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult("test-token");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult(true);

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult(false);
    }
}
