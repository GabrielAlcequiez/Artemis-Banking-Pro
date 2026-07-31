namespace ABP.Application.Features.Commerce.DTOs
{
    public sealed record ChangeCommerceStatusRequest(
        Guid CommerceId,
        bool IsActive);
}
