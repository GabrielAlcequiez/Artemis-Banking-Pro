namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record CashAdvanceRequest(
        Guid CreditCardId,
        Guid TargetAccountId,
        decimal Amount,
        Guid OperationId);
}
