namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record CreditCardPaymentRequest(
        Guid CreditCardId,
        Guid SourceAccountId,
        decimal Amount,
        Guid OperationId);
}
