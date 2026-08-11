using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApi.Models.CreditCards;

public sealed record CreditCardCreatedResponse(
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
    DateTimeOffset CreatedAt)
{
    public static CreditCardCreatedResponse From(CreditCardDetailDto detail) =>
        new(
            detail.Id,
            detail.MaskedCardNumber,
            detail.LastFourDigits,
            detail.ClientId,
            detail.ClientFullName,
            detail.CreditLimit,
            detail.AvailableCredit,
            detail.CurrentDebt,
            detail.ExpirationDate,
            detail.Status,
            detail.CreatedAt);
}
