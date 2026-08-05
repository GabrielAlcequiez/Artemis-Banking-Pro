namespace ABP.Application.DTOs.Commerce;

public sealed record CommerceSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc,
    bool IsActive,
    bool HasAssociatedUser,
    DateTimeOffset CreatedAt);
