namespace ABP.Domain.ReadModels.Commerce;

public sealed record AssociatedCommerceUserReadModel(
    string Id,
    string UserName,
    string Email,
    bool IsActive);
