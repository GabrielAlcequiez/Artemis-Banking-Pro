using ABP.Application.Features.Loans.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Loans;

public sealed class LoanPaymentViewModel
{
    public Guid LoanId { get; set; }

    public Guid SourceAccountId { get; set; }

    public decimal Amount { get; set; }

    public Guid OperationId { get; set; }

    public IReadOnlyCollection<LoanOperationOptionDto> Loans { get; set; } =
        Array.Empty<LoanOperationOptionDto>();

    public IReadOnlyCollection<SavingsAccountOperationOptionDto> SavingsAccounts { get; set; } =
        Array.Empty<SavingsAccountOperationOptionDto>();
}
