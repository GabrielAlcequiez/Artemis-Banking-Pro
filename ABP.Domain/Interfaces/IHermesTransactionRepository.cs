using ABP.Domain.Common;
using ABP.Domain.ReadModels.CreditCards;

namespace ABP.Domain.Interfaces;

public interface IHermesTransactionRepository
{
    Task<PagedResult<HermesTransactionReadModel>> GetByCommerceAsync(
        Guid commerceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
