using ABP.Application.Features.Commerce.DTOs;

namespace ABP.WebApi.Models.Commerce;

public sealed record CommerceCreatedResponse(
    Guid Id,
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    public static CommerceCreatedResponse From(CommerceDetailDto detail) =>
        new(
            detail.Id,
            detail.Name,
            detail.Description,
            detail.Email,
            detail.PhoneNumber,
            detail.Rnc,
            detail.IsActive,
            detail.CreatedAt);
}
