namespace ABP.Application.DTOs.Commerce;

public sealed record CommerceDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc,
    bool IsActive,
    DateTimeOffset CreatedAt,
    AssociatedCommerceUserDto? AssociatedUser);
