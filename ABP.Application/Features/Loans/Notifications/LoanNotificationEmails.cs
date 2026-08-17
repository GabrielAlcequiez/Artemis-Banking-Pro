using System.Globalization;
using System.Net;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Notifications;

internal static class LoanNotificationEmails
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
                "No se pudo preparar la notificación {NotificationType} de Loans para {ReferenceId}: destinatario sin correo disponible.",
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
                "No se pudo enviar la notificación {NotificationType} de Loans para {ReferenceId}. La operación permanece confirmada. Tipo de error: {ExceptionType}.",
                notificationType,
                referenceId,
                exception.GetType().Name);
            return false;
        }
    }

    public static EmailRequestDto LoanApproved(
        LoanNotificationRecipient recipient,
        string loanNumber,
        decimal approvedAmount,
        int termInMonths,
        decimal annualInterestRate,
        decimal monthlyInstallment) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = "Préstamo aprobado",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Su préstamo ha sido aprobado correctamente.</p>
                <p>Número de préstamo: {Encode(loanNumber)}</p>
                <p>Monto aprobado: RD${Encode(FormatAmount(approvedAmount))}</p>
                <p>Plazo: {termInMonths} meses</p>
                <p>Tasa de interés anual: {Encode(FormatPercentage(annualInterestRate))}%</p>
                <p>Cuota mensual: RD${Encode(FormatAmount(monthlyInstallment))}</p>
                <p>El monto aprobado ha sido depositado en su cuenta de ahorro principal.</p>
                """
        };

    public static EmailRequestDto RateChanged(
        LoanNotificationRecipient recipient,
        string loanNumber,
        decimal newAnnualInterestRate,
        decimal nextInstallmentAmount,
        DateOnly nextInstallmentDueDate) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = "Actualización de tasa de interés de préstamo",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>La tasa de interés de su préstamo {Encode(loanNumber)} ha sido actualizada.</p>
                <p>Nueva tasa de interés anual: {Encode(FormatPercentage(newAnnualInterestRate))}%</p>
                <p>Nuevo valor de la próxima cuota: RD${Encode(FormatAmount(nextInstallmentAmount))}</p>
                <p>Fecha de vencimiento de la próxima cuota: {Encode(FormatDate(nextInstallmentDueDate))}</p>
                """
        };

    public static EmailRequestDto LoanPayment(
        LoanNotificationRecipient recipient,
        string loanNumber,
        string sourceAccountLastFourDigits,
        decimal effectiveAmount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Pago realizado al préstamo {loanNumber}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se ha realizado un pago a su préstamo {Encode(loanNumber)}.</p>
                <p>Monto pagado: RD${Encode(FormatAmount(effectiveAmount))}</p>
                <p>Cuenta origen terminada en: {Encode(sourceAccountLastFourDigits)}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    public static EmailRequestDto PaymentAccountDebit(
        LoanNotificationRecipient recipient,
        string loanNumber,
        string sourceAccountLastFourDigits,
        decimal effectiveAmount,
        DateTimeOffset processedAtBankingTime) =>
        new()
        {
            ToEmail = recipient.Email,
            RecipientName = recipient.DisplayName,
            Subject = $"Débito para pago del préstamo {loanNumber}",
            Body = $"""
                <p>Hola {Encode(recipient.DisplayName)},</p>
                <p>Se debitó dinero de su cuenta terminada en {Encode(sourceAccountLastFourDigits)} para pagar el préstamo {Encode(loanNumber)}.</p>
                <p>Monto debitado: RD${Encode(FormatAmount(effectiveAmount))}</p>
                <p>Fecha y hora: {Encode(FormatDateTime(processedAtBankingTime))}</p>
                <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                """
        };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N2", DominicanCulture);

    private static string FormatPercentage(decimal percentage) =>
        percentage.ToString("N2", DominicanCulture);

    private static string FormatDate(DateOnly value) =>
        value.ToString("dd/MM/yyyy", DominicanCulture);

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("dd/MM/yyyy hh:mm:ss tt zzz", DominicanCulture);
}

internal sealed record LoanNotificationRecipient(
    string UserId,
    string Email,
    string DisplayName);
