namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record LoanPaymentRequest(
        Guid LoanId,
        Guid SourceAccountId,
        decimal Amount,
        Guid OperationId);
}
