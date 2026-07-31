using ABP.Application.Common;
using ABP.Application.Interfaces.Services;

namespace ABP.TestDoubles
{
    public class FakeCommerceAuthorizationResolverService : ICommerceAuthorizationResolverService
    {
        public OperationResult<Guid>? DefaultResult { get; set; }

        private readonly Dictionary<Guid, OperationResult<Guid>> _configuredResults = new();

        public void SetResultForCommerce(Guid requestedCommerceId, OperationResult<Guid> result)
        {
            _configuredResults[requestedCommerceId] = result;
        }

        public Task<OperationResult<Guid>> ResolveAuthorizedCommerceIdAsync(
            Guid requestedCommerceId,
            CancellationToken cancellationToken = default)
        {
            if (_configuredResults.TryGetValue(requestedCommerceId, out var result))
            {
                return Task.FromResult(result);
            }

            return Task.FromResult(DefaultResult ?? OperationResult<Guid>.Success(requestedCommerceId));
        }
    }
}
