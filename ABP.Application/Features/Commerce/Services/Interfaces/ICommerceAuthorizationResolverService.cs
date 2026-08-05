using ABP.Application.Common;

namespace ABP.Application.Features.Commerce.Services.Interfaces
{
    public interface ICommerceAuthorizationResolverService
    {
        Task<OperationResult<Guid>> ResolveAuthorizedCommerceIdAsync(
        Guid requestedCommerceId,
        CancellationToken cancellationToken = default);
    }
}