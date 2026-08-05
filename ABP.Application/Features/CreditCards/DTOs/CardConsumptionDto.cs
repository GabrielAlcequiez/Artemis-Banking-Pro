namespace ABP.Application.Features.CreditCards.DTOs
{
    public sealed record CardConsumptionDto(
    Guid Id,
    DateTimeOffset Date,
    decimal Amount,
    string CommerceName,
    string Status);
}
