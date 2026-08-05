namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record LoanPaymentResult(
        Guid LoanId,
        string LoanNumber,
        Guid SourceAccountId,
        decimal RequestedAmount,
        decimal EffectiveAmount,
        decimal RemainingLoanAmount,
        bool IsCompleted,
        Guid OperationId,
        DateTimeOffset ProcessedAt);
}
