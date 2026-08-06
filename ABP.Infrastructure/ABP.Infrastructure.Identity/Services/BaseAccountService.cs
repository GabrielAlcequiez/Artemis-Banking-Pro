using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
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
        private readonly ILogger<BaseAccountService> _logger;

        public BaseAccountService(IMapper mapper, UserManager<AppUser> userManager, IEmailService emailService, IValidator<CreateUserDto> createUserValidator, IValidator<EditUserDto> editUserValidator, IUserRepository userRepository, IUnitOfWork unitOfWork, IAccountTokenService accountTokenService, IPrimaryAccountProvisioner primaryAccountProvisioner, ILogger<BaseAccountService> logger)
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

            var exitsUserByIdentification = await _userRepository.GetByIdentificationAsync(createUserDto.Identification);
            if (exitsUserByIdentification)
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

        public Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string? origin, bool isApi = false)
        {
            throw new NotImplementedException();
        }


        public Task<UserResponseDto> ChangeUserStatusAsync(string userId, bool isActive)
        {
            throw new NotImplementedException();
        }

        public Task<string> ConfirmAccountAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }


        public Task<string> ForgotPasswordAsync(string username, string? origin = null, bool isApi = false)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<GetUserDto>> GetAllUsersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<GetUserDto?> GetUserByIdAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<GetUserDto?> GetUserByUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter)
        {
            throw new NotImplementedException();
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


        #endregion
    }
}