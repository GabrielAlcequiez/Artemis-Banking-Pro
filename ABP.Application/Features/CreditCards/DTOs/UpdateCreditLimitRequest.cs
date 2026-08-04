namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record UpdateCreditLimitRequest(
        Guid CreditCardId,
        decimal CreditLimit);
}
