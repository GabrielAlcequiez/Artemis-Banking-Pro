namespace ABP.Application.Features.Commerce.DTOs
{
    public sealed record CreateCommerceRequest(
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc);
}
