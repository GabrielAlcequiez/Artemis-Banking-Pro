using System.Globalization;
using System.Net;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Notifications;

internal static class AccountNotificationEmails
{
    private static readonly CultureInfo DominicanCulture =
        CultureInfo.GetCultureInfo("es-DO");

    public static async Task<bool> SendBestEffortAsync(
        IEmailService emailService,
        ILogger logger,
        EmailRequestDto email,
        string notificationType,
        string referenceId)
    {
        if (string.IsNullOrWhiteSpace(email.ToEmail))
        {
            logger.LogWarning(
                "No se pudo preparar la notificación {NotificationType} de Accounts para {ReferenceId}: destinatario sin correo disponible.",
                notificationType,
                referenceId);
            return false;
        }

        try
        {
            await emailService.SendAsync(email);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "No se pudo enviar la notificación {NotificationType} de Accounts para {ReferenceId}. La operación permanece confirmada. Tipo de error: {ExceptionType}.",
                notificationType,
                referenceId,
                exception.GetType().Name);
            return false;
        }
    }

    /// <summary>Sent to the account the money left, for Express/Beneficiary/ThirdParty transfers.</summary>
    public static EmailRequestDto TransferSent(
        AccountNotificationRecipient recipient,
        string destinationAccountLastFourDigits,
        decimal amount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Transacción realizada a la cuenta {destinationAccountLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado una transacción desde su cuenta hacia la cuenta terminada en {Encode(destinationAccountLastFourDigits)}.</p>
                <p>Monto transferido: RD${Encode(FormatAmount(amount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    /// <summary>Sent to the account the money arrived at, for Express/Beneficiary/ThirdParty transfers.</summary>
    public static EmailRequestDto TransferReceived(
        AccountNotificationRecipient recipient,
        string sourceAccountLastFourDigits,
        decimal amount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Transacción enviada desde la cuenta {sourceAccountLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Ha recibido una transacción desde la cuenta terminada en {Encode(sourceAccountLastFourDigits)}.</p>
                <p>Monto recibido: RD${Encode(FormatAmount(amount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto OwnAccountTransfer(
        AccountNotificationRecipient recipient,
        string sourceAccountLastFourDigits,
        string destinationAccountLastFourDigits,
        decimal amount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = "Transferencia entre cuentas realizada",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado una transferencia entre sus cuentas de ahorro.</p>
                <p>Cuenta origen terminada en: {Encode(sourceAccountLastFourDigits)}</p>
                <p>Cuenta destino terminada en: {Encode(destinationAccountLastFourDigits)}</p>
                <p>Monto transferido: RD${Encode(FormatAmount(amount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto Deposit(
        AccountNotificationRecipient recipient,
        string destinationAccountLastFourDigits,
        decimal amount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Depósito realizado a su cuenta {destinationAccountLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado un depósito a su cuenta terminada en {Encode(destinationAccountLastFourDigits)}.</p>
                <p>Monto depositado: RD${Encode(FormatAmount(amount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto Withdrawal(
        AccountNotificationRecipient recipient,
        string sourceAccountLastFourDigits,
        decimal amount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Retiro realizado desde su cuenta {sourceAccountLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado un retiro desde su cuenta terminada en {Encode(sourceAccountLastFourDigits)}.</p>
                <p>Monto retirado: RD${Encode(FormatAmount(amount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N2", DominicanCulture);

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("dd/MM/yyyy hh:mm:ss tt zzz", DominicanCulture);
}

internal sealed record AccountNotificationRecipient(
    string UserId,
    string Email,
    string DisplayName);
