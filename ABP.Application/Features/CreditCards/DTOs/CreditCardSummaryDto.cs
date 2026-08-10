namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record CreditCardSummaryDto(
    Guid Id,
    string MaskedCardNumber,
    string LastFourDigits,
    string ClientId,
    string ClientFullName,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal CurrentDebt,
    string ExpirationDate,
    string Status,
    DateTimeOffset CreatedAt);
}
