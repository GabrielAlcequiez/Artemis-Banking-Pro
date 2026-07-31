namespace ABP.Application.Features.Commerce.DTOs
{
    public sealed record UpdateCommerceRequest(
        Guid CommerceId,
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc);
}
