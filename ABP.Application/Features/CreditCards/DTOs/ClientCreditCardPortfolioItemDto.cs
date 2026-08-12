namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record ClientCreditCardPortfolioItemDto(
    Guid Id,
    string MaskedCardNumber,
    decimal CreditLimit,
    decimal CurrentDebt,
    string ExpirationDate);
