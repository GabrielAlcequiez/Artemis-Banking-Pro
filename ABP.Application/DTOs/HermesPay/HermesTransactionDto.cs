namespace ABP.Application.DTOs.HermesPay
{
    public sealed record HermesTransactionDto(
    Guid Id,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string CardLastFourDigits,
    string Status);
}
