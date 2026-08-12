using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApp : BaseAccountService, IAccountServiceForWebApp
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IValidator<LoginDto> _loginDtoValidator;

        public AccountServiceForWebApp(
            SignInManager<AppUser> signInManager,
            IValidator<LoginDto> loginDtoValidator,
            IMapper mapper,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            IValidator<CreateUserDto> createUserValidator,
            IValidator<EditUserDto> editUserValidator,
            IValidator<ResetPasswordDto> resetPasswordValidator,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IAccountTokenService accountTokenService,
            IPrimaryAccountProvisioner primaryAccountProvisioner,
            ISavingsAccountRepository savingsAccountRepository,
            IAccountBalanceService accountBalanceService,
            IAccountLedger accountLedger,
            ILogger<BaseAccountService> logger,
            ICommerceRepository commerceRepository,
            IValidator<CreateCommerceUserRequestDto> createCommerceUserValidator)
            : base(
                mapper,
                userManager,
                emailService,
                createUserValidator,
                editUserValidator,
                resetPasswordValidator,
                userRepository,
                unitOfWork,
                accountTokenService,
                primaryAccountProvisioner,
                savingsAccountRepository,
                accountBalanceService,
                accountLedger,
                logger,
                commerceRepository,
                createCommerceUserValidator)
        {
            _signInManager = signInManager;
            _loginDtoValidator = loginDtoValidator;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginRequestDto)
        {
            LoginResponseDto response = new()
            {
                HasError = false,
                Error = null
            };

            _logger.LogInformation("Iniciando proceso de inicio de sesión para el usuario {UserName}", loginRequestDto.Username);
            var validationResult = await _loginDtoValidator.ValidateAsync(loginRequestDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning(
                    "Error de validación al iniciar sesión con el usuario {Username}: {Errors}",
                    loginRequestDto.Username, string.Join("; ", errors));

                response.HasError = true;
                response.Error = string.Join("; ", errors);
                return response;
            }

            _logger.LogInformation("Verificando existencia del usuario {UserName}", loginRequestDto.Username);
            var appUser = await _userManager.FindByNameAsync(loginRequestDto.Username);
            if (appUser is null)
            {
                _logger.LogWarning("El usuario {UserName} no existe en el sistema.", loginRequestDto.Username);
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            _logger.LogInformation("Verificando si el usuario: {UserName} se encuentra activo.", loginRequestDto.Username);
            if (appUser.IsActive == false || appUser.EmailConfirmed == false)
            {
                _logger.LogWarning("El usuario: {UserName} se encuentra inactivo o no ha confirmado su correo electrónico.", loginRequestDto.Username);
                response.HasError = true;
                response.Error = "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.";
                return response;
            }

            _logger.LogInformation("Validando roles del usuario: {UserName}", loginRequestDto.Username);
            var userRoles = await _userManager.GetRolesAsync(appUser);

            var allowedRoles = new[] { Roles.Administrator.ToString(), Roles.Cashier.ToString(), Roles.Client.ToString() };
            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                _logger.LogInformation("El usuario: {UserName} no tiene un rol permitido para la aplicación web.", loginRequestDto.Username);
                response.HasError = true;
                response.Error = "Este usuario no tiene permisos para acceder a la aplicación web.";
                return response;
            }

            _logger.LogInformation("Intentando iniciar sesión para el usuario: {UserName}", loginRequestDto.Username);
            var result = await _signInManager.PasswordSignInAsync(loginRequestDto.Username, loginRequestDto.Password, false, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Los datos de acceso de este usuario en el sistema son inválidos.");
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            var domainUser = await _userRepository.GetByIdAsync(appUser.Id);
            if (domainUser is null)
            {
                _logger.LogError("El usuario de dominio del AppUser {UserId} no existe.", appUser.Id);
                response.HasError = true;
                response.Error = "No fue posible iniciar sesión. Intente nuevamente más tarde.";
                return response;
            }

            response = _mapper.Map<LoginResponseDto>(domainUser);
            response.Id = appUser.Id;
            response.IsVerified = appUser.EmailConfirmed;
            response.Roles = userRoles.ToList();

            _logger.LogInformation("Inicio de sesión exitoso para el usuario: {UserName}", loginRequestDto.Username);
            return response;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
