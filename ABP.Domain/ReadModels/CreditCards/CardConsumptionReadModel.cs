using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.CreditCards;

public sealed record CardConsumptionReadModel(
    Guid Id,
    DateTimeOffset Date,
    decimal Amount,
    string CommerceName,
    ConsumptionStatus Status);
