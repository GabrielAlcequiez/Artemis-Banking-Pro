namespace ABP.Application.DTOs.Commerce;

public sealed record AssociatedCommerceUserDto(
    string Id,
    string UserName,
    string Email,
    bool IsActive);
