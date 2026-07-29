namespace ABP.Application.Features.HermesPay.DTOs
{
    public sealed record HermesTransactionDto(
    Guid Id,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string CardLastFourDigits,
    string Status);
}