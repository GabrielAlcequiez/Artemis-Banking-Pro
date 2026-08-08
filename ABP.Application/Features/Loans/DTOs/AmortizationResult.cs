
namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record AmortizationResult(
        decimal MonthlyInstallment,
        decimal TotalAmountToPay,
        IReadOnlyCollection<LoanInstallmentDto> Installments);
}
