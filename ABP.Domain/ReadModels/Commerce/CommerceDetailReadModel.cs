using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.Commerce;

public sealed record CommerceDetailReadModel(
    Guid Id,
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc,
    CommerceStatus Status,
    DateTimeOffset CreatedAt,
    AssociatedCommerceUserReadModel? AssociatedUser);
