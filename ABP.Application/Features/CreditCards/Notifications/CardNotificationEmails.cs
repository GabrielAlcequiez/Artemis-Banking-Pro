using System.Globalization;
using System.Net;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.CreditCards.Notifications;

internal static class CardNotificationEmails
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
                "No se pudo preparar la notificación {NotificationType} de Cards para {ReferenceId}: destinatario sin correo disponible.",
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
                "No se pudo enviar la notificación {NotificationType} de Cards para {ReferenceId}. La operación permanece confirmada. Tipo de error: {ExceptionType}.",
                notificationType,
                referenceId,
                exception.GetType().Name);
            return false;
        }
    }

    public static EmailRequestDto Assignment(
        CardNotificationRecipient recipient,
        string cardLastFourDigits,
        decimal creditLimit,
        DateOnly expirationDate,
        DateTimeOffset assignedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = "Nueva tarjeta de crédito asignada",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha asignado una nueva tarjeta de crédito a su cuenta.</p>
                <p>Tarjeta terminada en: {Encode(cardLastFourDigits)}</p>
                <p>Límite aprobado: RD${Encode(FormatAmount(creditLimit))}</p>
                <p>Fecha de expiración: {Encode(expirationDate.ToString("MM/yy", CultureInfo.InvariantCulture))}</p>
                <p>Fecha de asignación: {Encode(FormatDate(assignedAtBankingTime))}</p>
                <p>Por seguridad, no comparta la información de su tarjeta con terceros.</p>
                """
        };

    public static EmailRequestDto LimitChanged(
        CardNotificationRecipient recipient,
        string cardLastFourDigits,
        decimal newCreditLimit,
        DateTimeOffset changedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = "Modificación de límite de tarjeta",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>El límite de su tarjeta de crédito terminada en {Encode(cardLastFourDigits)} ha sido actualizado.</p>
                <p>Nuevo límite aprobado: RD${Encode(FormatAmount(newCreditLimit))}</p>
                <p>Fecha de modificación: {Encode(FormatDate(changedAtBankingTime))}</p>
                <p>Si usted no reconoce esta modificación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto CardPayment(
        CardNotificationRecipient recipient,
        string cardLastFourDigits,
        string sourceAccountLastFourDigits,
        decimal effectiveAmount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Pago realizado a la tarjeta {cardLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado un pago a su tarjeta de crédito terminada en {Encode(cardLastFourDigits)}.</p>
                <p>Monto pagado: RD${Encode(FormatAmount(effectiveAmount))}</p>
                <p>Cuenta origen terminada en: {Encode(sourceAccountLastFourDigits)}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto PaymentAccountDebit(
        CardNotificationRecipient recipient,
        string cardLastFourDigits,
        string sourceAccountLastFourDigits,
        decimal effectiveAmount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Débito para pago de tarjeta {cardLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se debitó dinero de su cuenta terminada en {Encode(sourceAccountLastFourDigits)} para pagar una tarjeta terminada en {Encode(cardLastFourDigits)}.</p>
                <p>Monto debitado: RD${Encode(FormatAmount(effectiveAmount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto CashAdvance(
        CardNotificationRecipient recipient,
        string cardLastFourDigits,
        string targetAccountLastFourDigits,
        decimal receivedAmount,
        decimal totalCharge,
        DateTimeOffset processedAtBankingTime)
    {
        var interestAmount = totalCharge - receivedAmount;

        return new EmailRequestDto
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Avance de efectivo desde la tarjeta {cardLastFourDigits}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado un avance de efectivo desde su tarjeta terminada en {Encode(cardLastFourDigits)}.</p>
                <p>Monto recibido: RD${Encode(FormatAmount(receivedAmount))}</p>
                <p>Interés (6.25%): RD${Encode(FormatAmount(interestAmount))}</p>
                <p>Total cargado: RD${Encode(FormatAmount(totalCharge))}</p>
                <p>Cuenta destino terminada en: {Encode(targetAccountLastFourDigits)}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N2", DominicanCulture);

    private static string FormatDate(DateTimeOffset value) =>
        value.ToString("dd/MM/yyyy", DominicanCulture);

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("dd/MM/yyyy hh:mm:ss tt zzz", DominicanCulture);
}

internal sealed record CardNotificationRecipient(
    string UserId,
    string Email,
    string DisplayName);
