using ABP.Application.Common.DTOs;
using ABP.Application.Common.DTOs.Users;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ABP.Infrastructure.Identity.Services;

public partial class BaseAccountService
{
    private async Task<string?> SendResetPasswordEmail(string userId, string email, string token, string? origin, bool isApi)
    {
        _logger.LogInformation("Enviando correo de restablecimiento de contraseña al usuario {UserId}.", userId);

        try
        {
            if (!isApi)
            {
                string resetPasswordUri = $"{origin}/Account/ResetPassword?userId={userId}&token={Uri.EscapeDataString(token)}";
                await _emailService.SendAsync(new EmailRequestDto
                {
                    ToEmail = email,
                    RecipientName = "Usuario",
                    Subject = "Restablecimiento de Contraseña - Artemis Banking",
                    Body = $"Hola,<br/><br/>Se ha solicitado un restablecimiento de contraseña para su cuenta.<br/>Para restablecerla, utilice el siguiente enlace:<br/><a href='{resetPasswordUri}'>{resetPasswordUri}</a>"
                });
            }
            else
            {
                await _emailService.SendAsync(new EmailRequestDto
                {
                    ToEmail = email,
                    RecipientName = "Usuario",
                    Subject = "Token de Restablecimiento de Contraseña - Artemis Banking",
                    Body = $"Hola,<br/><br/>Se ha solicitado un restablecimiento de contraseña para su cuenta.<br/>Utilice el siguiente token para restablecer su contraseña desde la API:<br/><b>{token}</b>"
                });
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible enviar el correo de restablecimiento de contraseña al usuario {UserId}.", userId);
            return "No fue posible enviar el correo de restablecimiento de contraseña. Intente nuevamente más tarde.";
        }
    }

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
}