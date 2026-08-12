using ABP.Domain.Common;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Commerce;

namespace ABP.Domain.Interfaces;

public interface ICommerceRepository : IGenericRepository<Commerce, Guid>
{
    Task<bool> EmailExistsAsync(string email, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default);
    Task<bool> RncExistsAsync(string rnc, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<CommerceSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        CommerceStatusFilter? status = null,
        CancellationToken cancellationToken = default);

    Task<CommerceDetailReadModel?> GetDetailsAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default);

    Task<Commerce?> GetForUpdateAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default);
}
