using System.Transactions;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanDelinquencyService(
    ILoanRepository repository,
    IClock clock,
    ILogger<LoanDelinquencyService> logger) : ILoanDelinquencyService
{
    public async Task<int> UpdateDelinquencyAsync(DateOnly bankingDate, CancellationToken cancellationToken = default)
    {
        var modifiedAtUtc = clock.UtcNow;
        int updatedInstallments;

        using (var transaction = new TransactionScope(
                   TransactionScopeOption.Required,
                   new TransactionOptions
                   {
                       IsolationLevel = IsolationLevel.ReadCommitted
                   },
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            var markedInstallments = await repository.MarkOverdueInstallmentsAsync(
                bankingDate,
                modifiedAtUtc,
                cancellationToken);
            var clearedInstallments = await repository
                .ClearLateFlagFromPaidInstallmentsAsync(
                    null,
                    modifiedAtUtc,
                    null,
                    cancellationToken);
            updatedInstallments = markedInstallments + clearedInstallments;
            transaction.Complete();
        }

        if (updatedInstallments == 0)
        {
            logger.LogInformation(
                "El proceso de mora no encontró cuotas para actualizar en la fecha bancaria {BankingDate}.",
                bankingDate);

            return 0;
        }

        logger.LogInformation(
            "El proceso de mora actualizó {InstallmentCount} cuotas en la fecha bancaria {BankingDate}.",
            updatedInstallments,
            bankingDate);

        return updatedInstallments;
    }
}
