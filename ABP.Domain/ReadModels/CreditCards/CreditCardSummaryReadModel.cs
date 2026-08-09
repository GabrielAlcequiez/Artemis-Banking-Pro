using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.CreditCards;

public sealed record CreditCardSummaryReadModel(
    Guid Id,
    string MaskedCardNumber,
    string LastFourDigits,
    string ClientId,
    string ClientFullName,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal CurrentDebt,
    DateOnly ExpirationDate,
    CreditCardStatus Status,
    DateTimeOffset CreatedAt);
