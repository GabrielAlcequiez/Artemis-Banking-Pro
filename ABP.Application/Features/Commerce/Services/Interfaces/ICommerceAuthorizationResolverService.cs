using ABP.Application.Common;

namespace ABP.Application.Interfaces.Services
{
    public interface ICommerceAuthorizationResolverService
    {
        Task<OperationResult<Guid>> ResolveAuthorizedCommerceIdAsync(
        Guid requestedCommerceId,
        CancellationToken cancellationToken = default);
    }
}