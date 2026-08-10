using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Exceptions;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApi : IAccountServiceForWebApi
    {
        private const string InvalidCredentialsMessage = "No tiene autorización para acceder a este recurso.";
        private const string InactiveAccountMessage = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";
        private const string ForbiddenRoleMessage = "Acceso denegado. No tiene permisos para utilizar este recurso.";

        private static readonly HashSet<string> AllowedApiRoles =
        [
            Roles.Administrator.ToString(),
            Roles.Commerce.ToString()
        ];

        private readonly ILogger<AccountServiceForWebApi> _logger;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IValidator<LoginDto> _loginDtoValidator;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;

        public AccountServiceForWebApi(
            ILogger<AccountServiceForWebApi> logger,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IValidator<LoginDto> loginDtoValidator,
            IUserRepository userRepository,
            IJwtTokenService jwtTokenService)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _loginDtoValidator = loginDtoValidator;
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthenticationResponseDto> LoginAsync(LoginDto loginRequestDto)
        {
            await _loginDtoValidator.ValidateAndThrowAsync(loginRequestDto);

            var appUser = await _userManager.FindByNameAsync(loginRequestDto.Username);
            if (appUser is null)
            {
                _logger.LogWarning("Intento de inicio de sesión en la API con un usuario inexistente {UserName}.", loginRequestDto.Username);
                throw new ApiException(InvalidCredentialsMessage, StatusCodes.Status401Unauthorized);
            }

            if (!appUser.IsActive || !appUser.EmailConfirmed)
            {
                _logger.LogWarning("Intento de inicio de sesión en la API con cuenta inactiva o correo sin confirmar para el usuario {UserName}.", loginRequestDto.Username);
                throw new ApiException(InactiveAccountMessage, StatusCodes.Status401Unauthorized);
            }

            var userRoles = await _userManager.GetRolesAsync(appUser);
            if (!userRoles.Any(role => AllowedApiRoles.Contains(role)))
            {
                _logger.LogWarning("El usuario {UserName} intentó iniciar sesión en la API sin un rol permitido.", loginRequestDto.Username);
                throw new ApiException(ForbiddenRoleMessage, StatusCodes.Status403Forbidden);
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(appUser, loginRequestDto.Password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Credenciales inválidas al iniciar sesión en la API para el usuario {UserName}.", loginRequestDto.Username);
                throw new ApiException(InvalidCredentialsMessage, StatusCodes.Status401Unauthorized);
            }

            var domainUser = await _userRepository.GetByIdAsync(appUser.Id);
            if (domainUser is null)
            {
                _logger.LogError("El usuario de dominio del AppUser {UserId} no existe.", appUser.Id);
                throw new InvalidOperationException("No fue posible autenticar al usuario.");
            }

            var jwt = _jwtTokenService.GenerateToken(new TokenGenerationRequest
            {
                UserId = appUser.Id,
                UserName = appUser.UserName ?? string.Empty,
                Role = userRoles[0],
                CommerceId = domainUser.CommerceId
            });

            _logger.LogInformation("Inicio de sesión exitoso en la API para el usuario {UserName}.", loginRequestDto.Username);

            return new AuthenticationResponseDto { Jwt = jwt };
        }
    }
}
