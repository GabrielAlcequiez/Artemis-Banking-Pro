using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanDelinquencyService(ILoanRepository repository, IUnitOfWork unitOfWork, ILogger<LoanDelinquencyService> logger) : ILoanDelinquencyService
{
    public async Task<int> UpdateDelinquencyAsync(DateOnly bankingDate, CancellationToken cancellationToken = default)
    {
        var installments = await repository.GetInstallmentsForDelinquencyUpdateAsync(bankingDate, cancellationToken);
        var updatedInstallments = 0;

        foreach (var installment in installments)
        {
            var shouldBeLate = installment.DueDate < bankingDate
                && installment.PendingAmount > 0m;

            if (installment.IsLate == shouldBeLate)
            {
                continue;
            }

            installment.IsLate = shouldBeLate;
            updatedInstallments++;
        }

        if (updatedInstallments == 0)
        {
            logger.LogInformation(
                "El proceso de mora no encontró cuotas para actualizar en la fecha bancaria {BankingDate}.",
                bankingDate);

            return 0;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "El proceso de mora actualizó {InstallmentCount} cuotas en la fecha bancaria {BankingDate}.",
            updatedInstallments,
            bankingDate);

        return updatedInstallments;
    }
}
