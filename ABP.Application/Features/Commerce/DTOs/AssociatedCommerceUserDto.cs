namespace ABP.Application.Features.Commerce.DTOs;

public sealed record AssociatedCommerceUserDto(
    string Id,
    string UserName,
    string Email,
    bool IsActive);
