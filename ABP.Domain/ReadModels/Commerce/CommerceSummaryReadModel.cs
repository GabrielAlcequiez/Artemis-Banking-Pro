using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.Commerce;

public sealed record CommerceSummaryReadModel(
    Guid Id,
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc,
    CommerceStatus Status,
    bool HasAssociatedUser,
    DateTimeOffset CreatedAt);
