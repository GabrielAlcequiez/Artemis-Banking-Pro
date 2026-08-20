using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Notifications;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanRateService(
    ILoanRepository repository,
    IAmortizationCalculator amortizationCalculator,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<UpdateLoanRateRequest> validator,
    IEmailService emailService,
    ILogger<LoanRateService> logger) : ILoanRateService
{
    public async Task<LoanOperationResult> UpdateRateAsync(
        UpdateLoanRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var loan = await repository.GetWithInstallmentsAsync(
            request.LoanId,
            cancellationToken);

        if (loan is null)
        {
            logger.LogWarning(
                "No se pudo actualizar la tasa porque el préstamo {LoanId} no existe.",
                request.LoanId);

            return WithoutNotification(
                OperationResult.Failure(LoanErrors.NotFound));
        }

        if (loan.Status != LoanStatus.Active)
        {
            logger.LogWarning(
                "No se pudo actualizar la tasa del préstamo {LoanId} porque su estado es {LoanStatus}.",
                loan.Id,
                loan.Status);

            return WithoutNotification(
                OperationResult.Failure(LoanErrors.Inactive));
        }

        var today = clock.Today;
        var futurePendingInstallments = loan.Installments
            .Where(installment =>
                installment.PaymentStatus == InstallmentPaymentStatus.Pending
                && !installment.IsLate
                && installment.DueDate > today)
            .OrderBy(installment => installment.Number)
            .ToArray();

        if (futurePendingInstallments.Length == 0)
        {
            logger.LogWarning(
                "No se pudo actualizar la tasa del préstamo {LoanId} porque no tiene cuotas futuras pendientes.",
                loan.Id);

            return WithoutNotification(
                OperationResult.Failure(
                    LoanErrors.NoFuturePendingInstallments));
        }

        var capitalToRecalculate = futurePendingInstallments.Sum(
            installment => installment.CapitalAmount);
        var recalculatedSchedule = amortizationCalculator.Calculate(
            capitalToRecalculate,
            request.AnnualInterestRate,
            futurePendingInstallments.Length,
            today);
        var recalculatedInstallments = recalculatedSchedule.Installments.ToArray();

        for (var index = 0; index < futurePendingInstallments.Length; index++)
        {
            var installment = futurePendingInstallments[index];
            var recalculated = recalculatedInstallments[index];

            installment.InstallmentAmount = recalculated.InstallmentAmount;
            installment.InterestAmount = recalculated.InterestAmount;
            installment.CapitalAmount = recalculated.CapitalAmount;
            installment.PendingAmount = recalculated.InstallmentAmount;
        }

        loan.AnnualInterestRate = request.AnnualInterestRate;
        loan.PendingAmount = loan.Installments.Sum(
            installment => installment.PendingAmount);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "La tasa del préstamo {LoanId} fue actualizada y se recalcularon {InstallmentCount} cuotas futuras.",
            loan.Id,
            futurePendingInstallments.Length);

        var nextInstallment = futurePendingInstallments[0];
        var recipient = new LoanNotificationRecipient(
            loan.ClientId,
            loan.Client?.Email ?? string.Empty,
            loan.Client is null
                ? string.Empty
                : $"{loan.Client.Name} {loan.Client.LastName}".Trim());
        var notificationSent = await LoanNotificationEmails.SendBestEffortAsync(
            emailService,
            logger,
            LoanNotificationEmails.RateChanged(
                recipient,
                loan.LoanNumber,
                request.AnnualInterestRate,
                nextInstallment.InstallmentAmount,
                nextInstallment.DueDate),
            "actualización de tasa",
            loan.Id.ToString("N"));

        return new LoanOperationResult(
            OperationResult.Success(),
            !notificationSent);
    }

    private static LoanOperationResult WithoutNotification(
        OperationResult result) =>
        new(result, false);
}
