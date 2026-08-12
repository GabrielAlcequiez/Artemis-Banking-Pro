namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CashierCardPaymentPreview(
    Guid CreditCardId,
    Guid SourceAccountId,
    Guid OperationId,
    string AccountOwnerFullName,
    string AccountNumber,
    string CardOwnerFullName,
    string CardLastFourDigits,
    decimal RequestedAmount,
    decimal EffectiveAmount);
