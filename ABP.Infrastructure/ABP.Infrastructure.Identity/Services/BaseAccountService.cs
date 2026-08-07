using System.Transactions;
using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services
{
    public class BaseAccountService : IBaseAccountService
    {
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        private readonly IValidator<EditUserDto> _editUserValidator;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountTokenService _accountTokenService;
        private readonly IPrimaryAccountProvisioner _primaryAccountProvisioner;
        private readonly ISavingsAccountRepository _savingsAccountRepository;
        private readonly IAccountBalanceService _accountBalanceService;
        private readonly IAccountLedger _accountLedger;
        private readonly ILogger<BaseAccountService> _logger;

        public BaseAccountService(IMapper mapper, UserManager<AppUser> userManager, IEmailService emailService, IValidator<CreateUserDto> createUserValidator, IValidator<EditUserDto> editUserValidator, IUserRepository userRepository, IUnitOfWork unitOfWork, IAccountTokenService accountTokenService, IPrimaryAccountProvisioner primaryAccountProvisioner, ISavingsAccountRepository savingsAccountRepository, IAccountBalanceService accountBalanceService, IAccountLedger accountLedger, ILogger<BaseAccountService> logger)
        {
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
            _createUserValidator = createUserValidator;
            _editUserValidator = editUserValidator;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _accountTokenService = accountTokenService;
            _primaryAccountProvisioner = primaryAccountProvisioner;
            _savingsAccountRepository = savingsAccountRepository;
            _accountBalanceService = accountBalanceService;
            _accountLedger = accountLedger;
            _logger = logger;
        }

        public async Task<RegisterResponseDto> RegisterUserAsync(CreateUserDto createUserDto, string? origin, bool isApi = false)
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

            // Normaliza el label del formulario (Administrador/Cajero/Cliente) al nombre del enum,
            // que es también el nombre del rol sembrado en Identity (Administrator/Cashier/Client).
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
                    Error = "Ya existe un usuario registrado con este número de cédula.",
                    ErrorList = new List<string> { "Ya existe un usuario registrado con este número de cédula." }
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

            string? emailError = await SendActivationEmailAsync(appUser.Id, createUserDto, token, origin, isApi);

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

        public async Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string currentUserId, string? origin = null, bool isApi = false)
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

            if (editUserDto.AdditionalAmount.GetValueOrDefault() > 0 && domainUser.Role == Roles.Client)
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
                    Error = "El usuario seleccionado no existe."
                };
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            appUser.IsActive = isActive;
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

        public Task<string> ConfirmAccountAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }


        public Task<string> ForgotPasswordAsync(string username, string? origin = null, bool isApi = false)
        {
            throw new NotImplementedException();
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

        public async Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter)
        {
            _logger.LogInformation("Consultando usuarios paginados (página {Page}, tamaño {PageSize}, rol {Role}, solo comercio {IsCommerceOnly}).",
                filter.Page, filter.PageSize, filter.Role, filter.IsCommerceOnly);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

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

            return new PagedResultDto<GetUserDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords,
                TotalPages = result.TotalPages,
                Data = _mapper.Map<List<GetUserDto>>(result.Data)
            };
        }

        public Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            throw new NotImplementedException();
        }


        #region Helpers methods

        private async Task<string?> SendActivationEmailAsync(string userId, CreateUserDto createUserDto, string token, string? origin, bool isApi)
        {
            try
            {
                if (!isApi)
                {
                    string verificationUri = $"{origin}/Account/ConfirmAccount?userId={userId}&token={Uri.EscapeDataString(token)}";
                    await _emailService.SendAsync(new EmailRequestDto
                    {
                        ToEmail = createUserDto.Email,
                        RecipientName = $"{createUserDto.FirstName} {createUserDto.LastName}",
                        Subject = "Activación de Cuenta - Artemis Banking",
                        Body = $"Hola {createUserDto.FirstName},<br/><br/>Su cuenta ha sido creada correctamente.<br/>Para activarla, utilice el siguiente enlace:<br/><a href='{verificationUri}'>{verificationUri}</a>"
                    });
                }
                else
                {
                    await _emailService.SendAsync(new EmailRequestDto
                    {
                        ToEmail = createUserDto.Email,
                        RecipientName = $"{createUserDto.FirstName} {createUserDto.LastName}",
                        Subject = "Token de Activación de Cuenta - Artemis Banking",
                        Body = $"Hola {createUserDto.FirstName},<br/><br/>Su cuenta ha sido creada correctamente.<br/>Utilice el siguiente token para activar su cuenta desde la API:<br/><b>{token}</b>"
                    });
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar el correo de activación al usuario {UserId}.", userId);
                return "No fue posible enviar el correo de activación. Intente nuevamente más tarde.";
            }
        }

        private static Roles NormalizeRole(string role)
        {
            return role switch
            {
                "Administrador" => Roles.Administrator,
                "Cajero" => Roles.Cashier,
                "Cliente" => Roles.Client,
                _ when Enum.TryParse<Roles>(role, ignoreCase: true, out var parsed) => parsed,
                _ => throw new InvalidOperationException($"Rol no reconocido: {role}")
            };
        }

        private async Task InitializePrincipalAccountAsync(string ownerUserId, decimal? initialBalance)
        {
            var result = await _primaryAccountProvisioner.OpenPrincipalAccountAsync(
                ownerUserId,
                initialBalance ?? 0m,
                "system",
                Roles.Administrator.ToString());

            if (result.IsFailure)
            {
                _logger.LogError(
                    "No fue posible crear la cuenta de ahorro principal del usuario {UserId}: {ErrorCode} - {ErrorDescription}",
                    ownerUserId, result.Error.Code, result.Error.Description);
                throw new InvalidOperationException(result.Error.Description);
            }

            _logger.LogInformation(
                "Cuenta de ahorro principal creada para el usuario {UserId} con saldo inicial {InitialBalance}.",
                ownerUserId, initialBalance ?? 0m);
        }

        // Método privado para acreditar un monto adicional a la cuenta de ahorro principal del usuario usado en EditUserAsync
        private async Task ApplyAdditionalAmountAsync(User domainUser, decimal amount, string actorUserId, string actorRole)
        {
            var principalAccount = await _savingsAccountRepository.GetPrincipalAccountAsync(domainUser.Id);
            if (principalAccount is null)
            {
                _logger.LogError("El usuario {UserId} de tipo Cliente no tiene cuenta de ahorro principal activa.", domainUser.Id);
                throw new InvalidOperationException("El cliente no tiene una cuenta de ahorro principal activa.");
            }

            var creditResult = await _accountBalanceService.CreditAsync(principalAccount.Id, amount);
            if (creditResult.IsFailure)
            {
                _logger.LogError("No fue posible acreditar el monto adicional a la cuenta {AccountId}: {ErrorCode} - {ErrorDescription}",
                    principalAccount.Id, creditResult.Error.Code, creditResult.Error.Description);
                throw new InvalidOperationException(creditResult.Error.Description);
            }

            var operationId = Guid.NewGuid();
            await _accountLedger.RecordApprovedAsync(
                operationId, principalAccount.Id, amount,
                TransactionDirection.Credit, FinancialOperationType.AdministrativeCredit,
                "Monto adicional por edición de usuario", null, actorUserId, actorRole);

            _logger.LogInformation("Monto adicional de {Amount} acreditado a la cuenta principal {AccountNumber} del usuario {UserId}.",
                amount, principalAccount.AccountNumber, domainUser.Id);
        }

        #endregion
    }
}