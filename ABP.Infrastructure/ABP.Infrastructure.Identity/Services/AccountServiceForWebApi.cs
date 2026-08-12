using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Exceptions;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApi : BaseAccountService, IAccountServiceForWebApi
    {
        private const string InvalidCredentialsMessage = "No tiene autorización para acceder a este recurso.";
        private const string InactiveAccountMessage = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";
        private const string ForbiddenRoleMessage = "Acceso denegado. No tiene permisos para utilizar este recurso.";

        private static readonly HashSet<string> AllowedApiRoles =
        [
            Roles.Administrator.ToString(),
            Roles.Commerce.ToString()
        ];

        private readonly SignInManager<AppUser> _signInManager;
        private readonly IValidator<LoginDto> _loginDtoValidator;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IValidator<ConfirmAccountRequestDto> _confirmAccountValidator;
        private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
        private readonly IValidator<ChangeUserStatusRequestDto> _changeUserStatusValidator;
        private readonly IValidator<UserQueryFilterDto> _userQueryFilterValidator;

        public AccountServiceForWebApi(
            SignInManager<AppUser> signInManager,
            IValidator<LoginDto> loginDtoValidator,
            IJwtTokenService jwtTokenService,
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
            IValidator<CreateCommerceUserRequestDto> createCommerceUserValidator,
            IValidator<ConfirmAccountRequestDto> confirmAccountValidator,
            IValidator<ForgotPasswordDto> forgotPasswordValidator,
            IValidator<ChangeUserStatusRequestDto> changeUserStatusValidator,
            IValidator<UserQueryFilterDto> userQueryFilterValidator)
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
            _jwtTokenService = jwtTokenService;
            _confirmAccountValidator = confirmAccountValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _changeUserStatusValidator = changeUserStatusValidator;
            _userQueryFilterValidator = userQueryFilterValidator;
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

        public async Task ConfirmAccountAsync(ConfirmAccountRequestDto request)
        {
            await _confirmAccountValidator.ValidateAndThrowAsync(request);

            var error = await base.ConfirmAccountAsync(request.Token);
            if (!string.IsNullOrEmpty(error))
            {
                throw new ApiException(error, StatusCodes.Status400BadRequest);
            }
        }

        public async Task GetResetTokenAsync(ForgotPasswordDto forgotPasswordDto)
        {
            await _forgotPasswordValidator.ValidateAndThrowAsync(forgotPasswordDto);

            var error = await base.ForgotPasswordAsync(forgotPasswordDto, origin: null, isApi: true);
            if (!string.IsNullOrEmpty(error))
            {
                throw new ApiException(error, StatusCodes.Status400BadRequest);
            }
        }

        public override async Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var error = await base.ResetPasswordAsync(resetPasswordDto);
            if (!string.IsNullOrEmpty(error))
            {
                throw new ApiException(error, StatusCodes.Status400BadRequest);
            }

            return string.Empty;
        }

        public override async Task<RegisterResponseDto> RegisterUserAsync(CreateUserDto createUserDto, string? origin, bool isApi = false)
        {
            var response = await base.RegisterUserAsync(createUserDto, origin, isApi: true);

            if (response.IsConflict)
            {
                throw new ApiException(response.Error ?? "Conflicto con los datos del usuario.", StatusCodes.Status409Conflict);
            }

            if (response.HasError)
            {
                throw new ApiException(response.Error ?? "Solicitud inválida.", StatusCodes.Status400BadRequest);
            }

            return response;
        }

        public override async Task<RegisterResponseDto> RegisterCommerceUserAsync(CreateCommerceUserRequestDto createCommerceUserRequest, Guid commerceId, string? origin)
        {
            var response = await base.RegisterCommerceUserAsync(createCommerceUserRequest, commerceId, origin);

            if (response.IsNotFound)
            {
                throw new ApiException("El comercio indicado no existe.", StatusCodes.Status404NotFound);
            }

            if (response.IsConflict)
            {
                throw new ApiException(response.Error ?? "Conflicto con los datos del usuario de comercio.", StatusCodes.Status409Conflict);
            }

            if (response.HasError)
            {
                throw new ApiException(response.Error ?? "Solicitud inválida.", StatusCodes.Status400BadRequest);
            }

            return response;
        }

        public override async Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string currentUserId, string? origin = null, bool isApi = false)
        {
            var response = await base.EditUserAsync(editUserDto, currentUserId, origin, isApi: true);

            if (response.IsForbidden)
            {
                throw new ApiException("No puede editar su propia cuenta desde este módulo.", StatusCodes.Status403Forbidden);
            }

            if (response.IsNotFound)
            {
                throw new ApiException("El usuario seleccionado no existe.", StatusCodes.Status404NotFound);
            }

            if (response.IsConflict)
            {
                throw new ApiException(response.Error ?? "Conflicto con los datos del usuario.", StatusCodes.Status409Conflict);
            }

            if (response.HasError)
            {
                throw new ApiException(response.Error ?? "Solicitud inválida.", StatusCodes.Status400BadRequest);
            }

            return response;
        }

        public async Task ChangeUserStatusAsync(string userId, ChangeUserStatusRequestDto request, string currentUserId)
        {
            await _changeUserStatusValidator.ValidateAndThrowAsync(request);

            var response = await base.ChangeUserStatusAsync(userId, request.Status!.Value, currentUserId);

            if (response.IsForbidden)
            {
                throw new ApiException("No puede modificar el estado de su propia cuenta.", StatusCodes.Status403Forbidden);
            }

            if (response.IsNotFound)
            {
                throw new ApiException("El usuario seleccionado no existe.", StatusCodes.Status404NotFound);
            }

            if (response.HasError)
            {
                throw new ApiException(response.Error ?? "Solicitud inválida.", StatusCodes.Status400BadRequest);
            }
        }

        public override async Task<PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter)
        {
            await _userQueryFilterValidator.ValidateAndThrowAsync(filter);

            return await base.GetUsersPagedAsync(filter);
        }

        // La variante API nunca devuelve null: lanza ApiException(404) cuando el usuario no existe.
        public override async Task<UserDetailDto?> GetUserDetailAsync(string userId)
        {
            var detail = await base.GetUserDetailAsync(userId);
            if (detail is null)
            {
                throw new ApiException("El usuario seleccionado no existe.", StatusCodes.Status404NotFound);
            }

            return detail;
        }
    }
}