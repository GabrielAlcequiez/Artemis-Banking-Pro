using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.CreditCards;

public sealed record HermesTransactionReadModel(
    Guid Id,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string CardLastFourDigits,
    ConsumptionStatus Status);
