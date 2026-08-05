namespace ABP.Application.DTOs.CreditCards
{
    public sealed record CardConsumptionDto(
    Guid Id,
    DateTimeOffset Date,
    decimal Amount,
    string CommerceName,
    string Status);
}
