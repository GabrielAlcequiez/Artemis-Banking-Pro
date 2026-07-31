namespace ABP.Application.Common
{
    public sealed record FinancialOperationReceipt(
        Guid OperationId,
        decimal EffectiveAmount,
        DateTimeOffset ProcessedAtUtc);
}
