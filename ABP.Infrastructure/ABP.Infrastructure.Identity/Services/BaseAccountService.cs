using System.Transactions;
using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services
{
    public partial class BaseAccountService : IBaseAccountService
    {
        protected readonly IMapper _mapper;
        protected readonly UserManager<AppUser> _userManager;
        protected readonly IEmailService _emailService;
        protected readonly IValidator<CreateUserDto> _createUserValidator;
        protected readonly IValidator<EditUserDto> _editUserValidator;
        protected readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
        protected readonly IValidator<CreateCommerceUserRequestDto> _createCommerceUserValidator;
        protected readonly IUserRepository _userRepository;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IAccountTokenService _accountTokenService;
        protected readonly IPrimaryAccountProvisioner _primaryAccountProvisioner;
        protected readonly ISavingsAccountRepository _savingsAccountRepository;
        protected readonly IAccountBalanceService _accountBalanceService;
        protected readonly IAccountLedger _accountLedger;
        protected readonly ILogger<BaseAccountService> _logger;

        protected readonly ICommerceRepository _commerceRepository;

        public BaseAccountService(IMapper mapper, UserManager<AppUser> userManager, IEmailService emailService, IValidator<CreateUserDto> createUserValidator, IValidator<EditUserDto> editUserValidator, IValidator<ResetPasswordDto> resetPasswordValidator, IUserRepository userRepository, IUnitOfWork unitOfWork, IAccountTokenService accountTokenService, IPrimaryAccountProvisioner primaryAccountProvisioner, ISavingsAccountRepository savingsAccountRepository, IAccountBalanceService accountBalanceService, IAccountLedger accountLedger, ILogger<BaseAccountService> logger, ICommerceRepository commerceRepository, IValidator<CreateCommerceUserRequestDto> createCommerceUserValidator)
        {
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
            _createUserValidator = createUserValidator;
            _editUserValidator = editUserValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _accountTokenService = accountTokenService;
            _primaryAccountProvisioner = primaryAccountProvisioner;
            _savingsAccountRepository = savingsAccountRepository;
            _accountBalanceService = accountBalanceService;
            _accountLedger = accountLedger;
            _logger = logger;
            _commerceRepository = commerceRepository;
            _createCommerceUserValidator = createCommerceUserValidator;
        }

        public virtual async Task<RegisterResponseDto> RegisterUserAsync(CreateUserDto createUserDto, string? origin, bool isApi = false)
        {
            _logger.LogInformation("Iniciando solicitud de creación de usuario para {Username} con rol {Role} (isApi: {IsApi})", createUserDto.UserName, createUserDto.Role, isApi);

            // Si falla la validación
            var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning(
                    "Error de validación al registrar usuario {Username}: {Errors}",
                    createUserDto.UserName, string.Join("; ", errors));

                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    ErrorList = errors,
                    Error = string.Join("\n", errors)
                };
            }

            var role = NormalizeRole(createUserDto.Role);
            createUserDto.Role = role.ToString();

            #region Validaciones de Duplicados

            var userWithSameEmail = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (userWithSameEmail != null)
            {
                _logger.LogWarning("Intento de registro fallido: El correo {Email} ya está registrado.", createUserDto.Email);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con este correo electrónico.",
                    ErrorList = new List<string> { "Ya existe un usuario registrado con este correo electrónico." }
                };
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(createUserDto.UserName);
            if (userWithSameUserName != null)
            {
                _logger.LogWarning("Intento de registro fallido: El nombre de usuario {Username} ya existe.", createUserDto.UserName);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con este nombre de usuario.",
                    ErrorList = new List<string> { "Ya existe un usuario registrado con este nombre de usuario." }
                };
            }

            var exitsUserByIdentification = await _userRepository.FindByIdentificationAsync(createUserDto.Identification);
            if (exitsUserByIdentification is not null)
            {
                _logger.LogWarning("Intento de registro fallido: La cédula {Identification} ya está registrada.", createUserDto.Identification);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con esta cédula.",
                    ErrorList = new List<string> { "Ya existe un usuario registrado con esta cédula." }
                };
            }

            #endregion

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var isUserActive = false;

            var appUser = new AppUser
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                EmailConfirmed = isUserActive,
                IsActive = isUserActive
            };

            var result = await _userManager.CreateAsync(appUser, createUserDto.Password);
            if (!result.Succeeded)
            {
                var identityErrors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error de Identity al crear usuario {Username}: {Errors}", createUserDto.UserName, string.Join("; ", identityErrors));

                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    ErrorList = identityErrors,
                    Error = string.Join("\n", identityErrors)
                };
            }

            await _userManager.AddToRoleAsync(appUser, createUserDto.Role);

            var domainUser = new User(appUser.Id);
            _mapper.Map(createUserDto, domainUser);
            domainUser.IsActive = isUserActive;

            await _userRepository.AddAsync(domainUser);
            await _unitOfWork.SaveChangesAsync();

            if (role == Roles.Client)
            {
                await InitializePrincipalAccountAsync(appUser.Id, createUserDto.InitialBalance);
            }

            // Token de activación de cuenta
            string token = await _accountTokenService.GenerateAsync(appUser.Id, AccountTokenPurpose.Activation);

            scope.Complete();

            string? emailError = await SendActivationEmailAsync(appUser.Id, createUserDto.Email, $"{createUserDto.FirstName} {createUserDto.LastName}", createUserDto.FirstName, token, origin, isApi);

            if (emailError is not null)
            {
                return new RegisterResponseDto
                {
                    Id = appUser.Id,
                    HasError = true,
                    Error = emailError,
                    ErrorList = new List<string> { emailError }
                };
            }

            _logger.LogInformation("Usuario {Username} con ID {UserId} creado exitosamente en estado Inactivo.", createUserDto.UserName, appUser.Id);

            return new RegisterResponseDto
            {
                Id = appUser.Id,
                HasError = false,
                IsVerified = false
            };
        }

        public virtual async Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string currentUserId, string? origin = null, bool isApi = false)
        {
            _logger.LogInformation("Iniciando solicitud de edición del usuario {UserId}.", editUserDto.Id);

            var validationResult = await _editUserValidator.ValidateAsync(editUserDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Error de validación al editar usuario {UserId}: {Errors}", editUserDto.Id, string.Join("; ", errors));
                return new UserResponseDto
                {
                    HasError = true,
                    Error = string.Join("\n", errors)
                };
            }

            if (editUserDto.Id == currentUserId)
            {
                _logger.LogWarning("Intento de autoedición del usuario {UserId}.", currentUserId);
                return new UserResponseDto
                {
                    HasError = true,
                    IsForbidden = true,
                    Error = "No puede editar su propia cuenta desde este módulo."
                };
            }

            var appUser = await _userManager.FindByIdAsync(editUserDto.Id);
            if (appUser is null)
            {
                _logger.LogWarning("Intento de edición fallido: el usuario {UserId} no existe.", editUserDto.Id);
                return new UserResponseDto
                {
                    HasError = true,
                    IsNotFound = true,
                    Error = "El usuario seleccionado no existe."
                };
            }

            #region Validaciones de Duplicados (excluyendo self)

            if (!string.Equals(appUser.UserName, editUserDto.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var userWithSameUserName = await _userManager.FindByNameAsync(editUserDto.UserName);
                if (userWithSameUserName is not null && userWithSameUserName.Id != editUserDto.Id)
                {
                    _logger.LogWarning("Intento de edición fallido: el nombre de usuario {Username} ya pertenece a otro usuario.", editUserDto.UserName);
                    return new UserResponseDto
                    {
                        HasError = true,
                        IsConflict = true,
                        Error = "Ya existe otro usuario registrado con este nombre de usuario."
                    };
                }
            }

            if (!string.Equals(appUser.Email, editUserDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var userWithSameEmail = await _userManager.FindByEmailAsync(editUserDto.Email);
                if (userWithSameEmail is not null && userWithSameEmail.Id != editUserDto.Id)
                {
                    _logger.LogWarning("Intento de edición fallido: el correo {Email} ya pertenece a otro usuario.", editUserDto.Email);
                    return new UserResponseDto
                    {
                        HasError = true,
                        IsConflict = true,
                        Error = "Ya existe otro usuario registrado con este correo electrónico."
                    };
                }
            }

            var userWithSameIdentification = await _userRepository.FindByIdentificationAsync(editUserDto.Identification);
            if (userWithSameIdentification is not null && userWithSameIdentification.Id != editUserDto.Id)
            {
                _logger.LogWarning("Intento de edición fallido: la cédula {Identification} ya pertenece a otro usuario.", editUserDto.Identification);
                return new UserResponseDto
                {
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe otro usuario registrado con esta cédula."
                };
            }

            #endregion

            var domainUser = await _userRepository.GetByIdAsync(editUserDto.Id);
            if (domainUser is null)
            {
                _logger.LogWarning("Intento de edición fallido: el usuario de dominio {UserId} no existe.", editUserDto.Id);
                return new UserResponseDto
                {
                    HasError = true,
                    IsNotFound = true,
                    Error = "El usuario seleccionado no existe."
                };
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            appUser.UserName = editUserDto.UserName;
            appUser.Email = editUserDto.Email;
            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                var identityErrors = updateResult.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error al actualizar AppUser {UserId}: {Errors}", editUserDto.Id, string.Join("; ", identityErrors));
                return new UserResponseDto
                {
                    HasError = true,
                    Error = string.Join("\n", identityErrors)
                };
            }

            if (!string.IsNullOrEmpty(editUserDto.Password))
            {
                if (await _userManager.HasPasswordAsync(appUser))
                {
                    await _userManager.RemovePasswordAsync(appUser);
                }
                await _userManager.AddPasswordAsync(appUser, editUserDto.Password);
            }

            _mapper.Map(editUserDto, domainUser);
            await _userRepository.UpdateAsync(editUserDto.Id, domainUser);
            await _unitOfWork.SaveChangesAsync();

            if (editUserDto.AdditionalAmount.GetValueOrDefault() > 0 && domainUser.Role is Roles.Client or Roles.Commerce)
            {
                var actorRoles = await _userManager.GetRolesAsync(appUser);
                var actorRole = actorRoles.FirstOrDefault() ?? Roles.Administrator.ToString();
                await ApplyAdditionalAmountAsync(domainUser, editUserDto.AdditionalAmount!.Value, currentUserId, actorRole);
            }

            scope.Complete();

            _logger.LogInformation("Usuario {UserId} editado exitosamente.", editUserDto.Id);

            return new UserResponseDto();
        }


        public async Task<UserResponseDto> ChangeUserStatusAsync(string userId, bool isActive, string currentUserId)
        {
            _logger.LogInformation("Iniciando cambio de estado del usuario {UserId} a {IsActive}.", userId, isActive);

            if (userId == currentUserId)
            {
                _logger.LogWarning("Intento de cambio de estado del propio usuario {UserId}.", userId);
                return new UserResponseDto
                {
                    HasError = true,
                    IsForbidden = true,
                    Error = "No puede modificar el estado de su propia cuenta."
                };
            }

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser is null)
            {
                _logger.LogWarning("Intento de cambio de estado fallido: el usuario {UserId} no existe.", userId);
                return new UserResponseDto
                {
                    HasError = true,
                    IsNotFound = true,
                    Error = "El usuario seleccionado no existe."
                };
            }

            var domainUser = await _userRepository.GetByIdAsync(userId);
            if (domainUser is null)
            {
                _logger.LogWarning("Intento de cambio de estado fallido: el usuario de dominio {UserId} no existe.", userId);
                return new UserResponseDto
                {
                    HasError = true,
                    IsNotFound = true,
                    Error = "El usuario seleccionado no existe."
                };
            }

            if (isActive && domainUser.Role == Roles.Commerce)
            {
                var commerceActivationError = await GetCommerceActivationErrorAsync(domainUser);
                if (commerceActivationError is not null)
                {
                    return new UserResponseDto
                    {
                        HasError = true,
                        Error = commerceActivationError
                    };
                }

                if (!appUser.IsActive || !domainUser.IsActive)
                {
                    return new UserResponseDto
                    {
                        HasError = true,
                        Error = "Los usuarios de comercio deben reactivarse mediante confirmación o restablecimiento de contraseña."
                    };
                }

                // Ya está activo en ambos almacenes: la solicitud es idempotente.
                return new UserResponseDto();
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            appUser.IsActive = isActive;
            if (isActive)
            {
                appUser.EmailConfirmed = true;
            }
            var appUpdateResult = await _userManager.UpdateAsync(appUser);
            if (!appUpdateResult.Succeeded)
            {
                var identityErrors = appUpdateResult.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error al actualizar AppUser {UserId} para cambio de estado: {Errors}", userId, string.Join("; ", identityErrors));
                return new UserResponseDto
                {
                    HasError = true,
                    Error = string.Join("\n", identityErrors)
                };
            }

            domainUser.IsActive = isActive;
            await _userRepository.UpdateAsync(userId, domainUser);
            await _unitOfWork.SaveChangesAsync();

            scope.Complete();

            _logger.LogInformation("Estado del usuario {UserId} cambiado a {IsActive}.", userId, isActive);

            return new UserResponseDto();
        }

        public async Task<string> ConfirmAccountAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("Intento de confirmación de cuenta fallido: el usuario {UserId} no existe.", userId);
                return "El usuario no existe.";
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogInformation("El usuario {UserId} ya ha confirmado su cuenta previamente.", userId);
                return "La cuenta ya ha sido confirmada previamente.";
            }

            var validationResult = await _accountTokenService.ValidateAsync(userId, token, AccountTokenPurpose.Activation);

            if (validationResult.Status != AccountTokenValidationStatus.Valid)
            {
                _logger.LogWarning("Intento de confirmación de cuenta fallido para el usuario {UserId}: Token inválido o expirado.", userId);
                return validationResult.Status switch
                {
                    AccountTokenValidationStatus.NotFound => "El token de activación no fue encontrado.",
                    AccountTokenValidationStatus.Used => "El token de activación ya ha sido utilizado.",
                    AccountTokenValidationStatus.Expired => "El token de activación ha expirado.",
                    AccountTokenValidationStatus.Invalid => "El token de activación es inválido.",
                    _ => "Error desconocido al validar el token de activación."
                };
            }

            var domainUser = await _userRepository.GetByIdAsync(userId);
            if (domainUser is null)
            {
                _logger.LogWarning("Intento de confirmación fallido: el usuario de dominio {UserId} no existe.", userId);
                return "El usuario no existe.";
            }

            var commerceActivationError = await GetCommerceActivationErrorAsync(domainUser);
            if (commerceActivationError is not null)
            {
                return commerceActivationError;
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (!await _accountTokenService.TryMarkAsUsedAsync(validationResult.AccountTokenId!.Value))
            {
                return "El token de activación ya ha sido utilizado.";
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return "No fue posible confirmar la cuenta. El token no es válido.";
            }

            user.IsActive = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return string.Join("\n", updateResult.Errors.Select(error => error.Description));
            }

            domainUser.IsActive = true;
            await _userRepository.UpdateAsync(userId, domainUser);
            await _unitOfWork.SaveChangesAsync();

            scope.Complete();

            _logger.LogInformation("Cuenta del usuario {UserId} activada exitosamente.", userId);
            return string.Empty;
        }

        public async Task<string> ConfirmAccountAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Intento de confirmación de cuenta sin token.");
                return "El token de activación es obligatorio.";
            }

            var validationResult = await _accountTokenService.ValidateByTokenAsync(token, AccountTokenPurpose.Activation);

            if (validationResult.Status != AccountTokenValidationStatus.Valid || string.IsNullOrEmpty(validationResult.UserId))
            {
                _logger.LogWarning("Intento de confirmación de cuenta con token inválido o expirado.");
                return validationResult.Status switch
                {
                    AccountTokenValidationStatus.NotFound => "El token de activación no fue encontrado.",
                    AccountTokenValidationStatus.Used => "El token de activación ya ha sido utilizado.",
                    AccountTokenValidationStatus.Expired => "El token de activación ha expirado.",
                    AccountTokenValidationStatus.Invalid => "El token de activación es inválido.",
                    _ => "El token de activación no es válido."
                };
            }

            return await ConfirmAccountAsync(validationResult.UserId, token);
        }

        public async Task<string?> ValidateResetTokenAsync(string userId, string token)
        {
            _logger.LogInformation("Validando token de restablecimiento para el usuario {UserId}.", userId);

            var validationResult = await _accountTokenService.ValidateAsync(userId, token, AccountTokenPurpose.PasswordReset);

            return validationResult.Status switch
            {
                AccountTokenValidationStatus.Valid => null,
                AccountTokenValidationStatus.NotFound => "El enlace de restablecimiento no es válido.",
                AccountTokenValidationStatus.Invalid => "El enlace de restablecimiento no es válido.",
                AccountTokenValidationStatus.Expired => "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.",
                AccountTokenValidationStatus.Used => "Este enlace de restablecimiento ya fue utilizado.",
                _ => "El enlace de restablecimiento no es válido."
            };
        }

        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? origin = null, bool isApi = false)
        {
            _logger.LogInformation("Iniciando solicitud de recuperación de contraseña para el usuario {Username}.", forgotPasswordDto.Username);

            if (string.IsNullOrWhiteSpace(forgotPasswordDto.Username))
            {
                _logger.LogWarning("Solicitud de recuperación de contraseña sin nombre de usuario.");
                return "El nombre de usuario es obligatorio.";
            }

            var user = await _userManager.FindByNameAsync(forgotPasswordDto.Username);
            if (user is null)
            {
                _logger.LogWarning("Intento de recuperación de contraseña fallido: el usuario {Username} no existe.", forgotPasswordDto.Username);
                return "No existe un usuario registrado con este nombre de usuario.";
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Intento de recuperación de contraseña fallido: el usuario {UserId} no tiene correo electrónico registrado.", user.Id);
                return "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento.";
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allowedRoles = isApi
                ? new[] { Roles.Administrator.ToString(), Roles.Commerce.ToString() }
                : new[] { Roles.Administrator.ToString(), Roles.Cashier.ToString(), Roles.Client.ToString() };

            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                _logger.LogWarning("Intento de recuperación de contraseña fallido: el usuario {UserId} no tiene un rol permitido (isApi: {IsApi}).", user.Id, isApi);
                return isApi
                    ? "El usuario no pertenece a un rol permitido por la API."
                    : "El usuario no pertenece a un rol permitido para la aplicación web.";
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // Desactivar temporalmente la cuenta (se reactiva al completar el restablecimiento).
            // Se desactiva también a nivel de correo y se renueva el security stamp para
            // invalidar sesiones existentes y tokens de Identity emitidos previamente.
            user.IsActive = false;
            user.EmailConfirmed = false;
            var deactivateResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!deactivateResult.Succeeded)
            {
                var identityErrors = deactivateResult.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error al desactivar temporalmente el usuario {UserId}: {Errors}", user.Id, string.Join("; ", identityErrors));
                return string.Join("\n", identityErrors);
            }

            var domainUser = await _userRepository.GetByIdAsync(user.Id);
            if (domainUser is not null)
            {
                domainUser.IsActive = false;
                await _userRepository.UpdateAsync(user.Id, domainUser);
                await _unitOfWork.SaveChangesAsync();
            }

            // Genera y persiste el token (hash + expiración de 30 min + uso único) asociado al usuario.
            string token = await _accountTokenService.GenerateAsync(user.Id, AccountTokenPurpose.PasswordReset);

            scope.Complete();

            string? emailError = await SendResetPasswordEmail(user.Id, user.Email, token, origin, isApi);
            if (emailError is not null)
            {
                _logger.LogWarning("No fue posible enviar el correo de restablecimiento al usuario {UserId}: {Error}", user.Id, emailError);
                return emailError;
            }

            _logger.LogInformation("Token de restablecimiento de contraseña enviado al correo del usuario {Username}.", forgotPasswordDto.Username);

            return string.Empty;
        }

        public async Task<IReadOnlyList<GetUserDto>> GetAllUsersAsync()
        {
            _logger.LogInformation("Consultando todos los usuarios del sistema.");

            var users = await _userRepository.GetAllAsync(false);

            return _mapper.Map<IReadOnlyList<GetUserDto>>(users);
        }

        public async Task<GetUserDto?> GetUserByIdAsync(string userId)
        {
            _logger.LogInformation("Consultando el usuario {UserId}.", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            return _mapper.Map<GetUserDto>(user);
        }

        public virtual async Task<UserDetailDto?> GetUserDetailAsync(string userId)
        {
            _logger.LogInformation("Consultando el detalle del usuario {UserId}.", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            var detail = _mapper.Map<UserDetailDto>(user);

            var principal = await _savingsAccountRepository.GetPrincipalAccountAsync(userId);
            if (principal is not null)
            {
                detail.MainAccount = new UserMainAccountDto
                {
                    AccountNumber = principal.AccountNumber,
                    Balance = principal.Balance,
                    IsPrincipal = true,
                    Status = principal.Status.ToString()
                };
            }

            return detail;
        }

        public async Task<GetUserDto?> GetUserByUsernameAsync(string username)
        {
            _logger.LogInformation("Consultando el usuario con nombre de usuario {Username}.", username);

            var appUser = await _userManager.FindByNameAsync(username);
            if (appUser is null)
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(appUser.Id);
            if (user is null)
            {
                return null;
            }

            return _mapper.Map<GetUserDto>(user);
        }

        public virtual async Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter)
        {
            _logger.LogInformation("Consultando usuarios paginados (página {Page}, tamaño {PageSize}, rol {Role}, solo comercio {IsCommerceOnly}).",
                filter.Page, filter.PageSize, filter.Role, filter.IsCommerceOnly);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
            pageSize = pageSize > 20 ? 20 : pageSize;

            Roles? parsedRole = null;
            if (!string.IsNullOrEmpty(filter.Role) &&
                !string.Equals(filter.Role, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    parsedRole = NormalizeRole(filter.Role);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning("Rol de filtro inválido {Role} en la consulta paginada de usuarios.", filter.Role);
                    return new PagedResultDto<GetUserDto>
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalRecords = 0,
                        TotalPages = 0,
                        Data = new List<GetUserDto>()
                    };
                }
            }

            var result = await _userRepository.GetPagedAsync(
                new PagedRequest(page, pageSize),
                filter.IsCommerceOnly,
                parsedRole);

            var data = _mapper.Map<List<GetUserDto>>(result.Data);

            var commerceIds = data
                .Where(x => x.CommerceId.HasValue)
                .Select(x => x.CommerceId!.Value)
                .Distinct()
                .ToList();

            if (commerceIds.Count > 0)
            {
                var commerces = await _commerceRepository.GetAllAsync(false);
                var commerceNames = commerces
                    .Where(commerce => commerceIds.Contains(commerce.Id))
                    .ToDictionary(commerce => commerce.Id, commerce => commerce.Name);

                foreach (var item in data)
                {
                    if (item.CommerceId.HasValue &&
                        commerceNames.TryGetValue(item.CommerceId.Value, out var commerceName))
                    {
                        item.CommerceName = commerceName;
                    }
                }
            }

            return new PagedResultDto<GetUserDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords,
                TotalPages = result.TotalPages,
                Data = data
            };
        }

        public virtual async Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            _logger.LogInformation("Iniciando restablecimiento de contraseña para el usuario {UserId}.", resetPasswordDto.UserId);

            var validationResult = await _resetPasswordValidator.ValidateAsync(resetPasswordDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Error de validación al restablecer contraseña del usuario {UserId}: {Errors}", resetPasswordDto.UserId, string.Join("; ", errors));
                return string.Join("\n", errors);
            }

            var tokenValidation = await _accountTokenService.ValidateAsync(resetPasswordDto.UserId, resetPasswordDto.Token, AccountTokenPurpose.PasswordReset);

            if (tokenValidation.Status != AccountTokenValidationStatus.Valid)
            {
                _logger.LogWarning("Intento de restablecimiento de contraseña fallido para el usuario {UserId}: Token inválido o expirado.", resetPasswordDto.UserId);
                return tokenValidation.Status switch
                {
                    AccountTokenValidationStatus.NotFound => "El enlace de restablecimiento no es válido.",
                    AccountTokenValidationStatus.Invalid => "El enlace de restablecimiento no es válido.",
                    AccountTokenValidationStatus.Expired => "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.",
                    AccountTokenValidationStatus.Used => "Este enlace de restablecimiento ya fue utilizado.",
                    _ => "Error desconocido al validar el token de restablecimiento."
                };
            }

            var user = await _userManager.FindByIdAsync(resetPasswordDto.UserId);
            if (user is null)
            {
                _logger.LogWarning("Intento de restablecimiento de contraseña fallido: el usuario {UserId} no existe.", resetPasswordDto.UserId);
                return "El enlace de restablecimiento no es válido.";
            }

            var domainUser = await _userRepository.GetByIdAsync(resetPasswordDto.UserId);
            if (domainUser is null)
            {
                _logger.LogWarning("Intento de restablecimiento fallido: el usuario de dominio {UserId} no existe.", resetPasswordDto.UserId);
                return "El enlace de restablecimiento no es válido.";
            }

            var commerceActivationError = await GetCommerceActivationErrorAsync(domainUser);
            if (commerceActivationError is not null)
            {
                return commerceActivationError;
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (!await _accountTokenService.TryMarkAsUsedAsync(tokenValidation.AccountTokenId!.Value))
            {
                return "Este enlace de restablecimiento ya fue utilizado.";
            }

            var resetResult = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.Password);
            if (!resetResult.Succeeded)
            {
                var identityErrors = resetResult.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error al restablecer la contraseña del usuario {UserId}: {Errors}", resetPasswordDto.UserId, string.Join("; ", identityErrors));
                return string.Join("\n", identityErrors);
            }

            // Reactivar la cuenta (se desactivó temporalmente al solicitar el restablecimiento).
            user.IsActive = true;
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return string.Join("\n", updateResult.Errors.Select(error => error.Description));
            }

            domainUser.IsActive = true;
            await _userRepository.UpdateAsync(resetPasswordDto.UserId, domainUser);
            await _unitOfWork.SaveChangesAsync();

            scope.Complete();

            _logger.LogInformation("Contraseña del usuario {UserId} restablecida exitosamente.", resetPasswordDto.UserId);

            return string.Empty;
        }

        public virtual async Task<RegisterResponseDto> RegisterCommerceUserAsync(CreateCommerceUserRequestDto createCommerceUserRequest, Guid commerceId, string? origin)
        {
            _logger.LogInformation("Iniciando solicitud de creación de usuario de comercio para el comercio {CommerceId}.", commerceId);

            var validationResult = await _createCommerceUserValidator.ValidateAsync(createCommerceUserRequest);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning(
                    "Error de validación al registrar usuario de comercio para el comercio {CommerceId}: {Errors}",
                    commerceId, string.Join("; ", errors));

                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    ErrorList = errors,
                    Error = string.Join("\n", errors)
                };
            }

            // Verifica la existencia del comercio al que se asociará el usuario.
            var commerce = await _commerceRepository.GetByIdAsync(commerceId);
            if (commerce is null)
            {
                _logger.LogWarning("Intento de registro de usuario de comercio fallido: el comercio {CommerceId} no existe.", commerceId);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsNotFound = true,
                    Error = "El comercio indicado no existe."
                };
            }

            if (await _userRepository.ExistsByCommerceIdAsync(commerceId))
            {
                _logger.LogWarning("Intento de registro de usuario de comercio fallido: el comercio {CommerceId} ya tiene un usuario asociado.", commerceId);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "El comercio ya tiene un usuario asociado."
                };
            }

            #region Validaciones de Duplicados

            var userWithSameEmail = await _userManager.FindByEmailAsync(createCommerceUserRequest.Email);
            if (userWithSameEmail != null)
            {
                _logger.LogWarning("Intento de registro de usuario de comercio fallido: El correo {Email} ya está registrado.", createCommerceUserRequest.Email);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con este correo electrónico."
                };
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(createCommerceUserRequest.UserName);
            if (userWithSameUserName != null)
            {
                _logger.LogWarning("Intento de registro de usuario de comercio fallido: El nombre de usuario {Username} ya existe.", createCommerceUserRequest.UserName);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con este nombre de usuario."
                };
            }

            var exitsUserByIdentification = await _userRepository.FindByIdentificationAsync(createCommerceUserRequest.Identification);
            if (exitsUserByIdentification is not null)
            {
                _logger.LogWarning("Intento de registro de usuario de comercio fallido: La cédula {Identification} ya está registrada.", createCommerceUserRequest.Identification);
                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    IsConflict = true,
                    Error = "Ya existe un usuario registrado con esta cédula."
                };
            }

            #endregion

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var appUser = new AppUser
            {
                UserName = createCommerceUserRequest.UserName,
                Email = createCommerceUserRequest.Email,
                EmailConfirmed = false,
                IsActive = false
            };

            var result = await _userManager.CreateAsync(appUser, createCommerceUserRequest.Password);
            if (!result.Succeeded)
            {
                var identityErrors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Error de Identity al crear usuario de comercio {Username}: {Errors}", createCommerceUserRequest.UserName, string.Join("; ", identityErrors));

                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    ErrorList = identityErrors,
                    Error = string.Join("\n", identityErrors)
                };
            }

            var roleResult = await _userManager.AddToRoleAsync(appUser, Roles.Commerce.ToString());
            if (!roleResult.Succeeded)
            {
                var roleErrors = roleResult.Errors.Select(error => error.Description).ToList();
                _logger.LogWarning(
                    "No fue posible asignar el rol Comercio al usuario {UserId}: {Errors}",
                    appUser.Id,
                    string.Join("; ", roleErrors));

                return new RegisterResponseDto
                {
                    Id = string.Empty,
                    HasError = true,
                    ErrorList = roleErrors,
                    Error = string.Join("\n", roleErrors)
                };
            }

            var domainUser = new User(appUser.Id)
            {
                Name = createCommerceUserRequest.FirstName,
                LastName = createCommerceUserRequest.LastName,
                Email = createCommerceUserRequest.Email,
                UserName = createCommerceUserRequest.UserName,
                Identification = createCommerceUserRequest.Identification,
                Role = Roles.Commerce,
                IsActive = false,
                CommerceId = commerceId
            };

            await _userRepository.AddAsync(domainUser);
            await _unitOfWork.SaveChangesAsync();

            await InitializePrincipalAccountAsync(appUser.Id, createCommerceUserRequest.InitialAmount);

            // Token de activación de cuenta
            string token = await _accountTokenService.GenerateAsync(appUser.Id, AccountTokenPurpose.Activation);

            scope.Complete();

            string? emailError = await SendActivationEmailAsync(
                appUser.Id,
                createCommerceUserRequest.Email,
                $"{createCommerceUserRequest.FirstName} {createCommerceUserRequest.LastName}",
                createCommerceUserRequest.FirstName,
                token,
                origin,
                isApi: true);

            if (emailError is not null)
            {
                // Sin Outbox, el estado confirmado en base de datos es la fuente de verdad.
                // Reportar un fallo HTTP provocaría que el cliente reintentara una creación ya completada.
                _logger.LogWarning(
                    "El usuario de comercio {UserId} fue creado, pero el correo de activación no pudo enviarse.",
                    appUser.Id);
            }

            _logger.LogInformation("Usuario de comercio {Username} con ID {UserId} creado exitosamente en estado Inactivo.", createCommerceUserRequest.UserName, appUser.Id);

            return new RegisterResponseDto
            {
                Id = appUser.Id,
                HasError = false,
                IsVerified = false
            };
        }

        private async Task<string?> GetCommerceActivationErrorAsync(User domainUser)
        {
            if (domainUser.Role != Roles.Commerce)
            {
                return null;
            }

            if (domainUser.CommerceId is null)
            {
                return "El usuario de comercio no tiene un comercio asociado.";
            }

            var commerce = await _commerceRepository.GetByIdAsync(domainUser.CommerceId.Value);
            if (commerce is null)
            {
                return "El comercio asociado al usuario no existe.";
            }

            return commerce.Status == CommerceStatus.Active
                ? null
                : "El usuario de comercio no puede activarse mientras el comercio esté inactivo.";
        }
    }
}
