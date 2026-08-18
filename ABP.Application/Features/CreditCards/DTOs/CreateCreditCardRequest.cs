namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record CreateCreditCardRequest(
        string ClientId,
        decimal CreditLimit,
        Guid OperationId);
}
