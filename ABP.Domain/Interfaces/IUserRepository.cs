using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;

namespace ABP.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User, string>
    {
        Task<User?> FindByIdentificationAsync(string identification);

        Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default);

        Task<PagedResult<User>> GetActiveClientsPagedAsync(
            PagedRequest request,
            string? identification = null,
            CancellationToken cancellationToken = default);

        Task<User?> GetActiveClientByIdAsync(
            string clientId,
            CancellationToken cancellationToken = default);

        Task<int> CountActiveClientsAsync(
            CancellationToken cancellationToken = default);
    }
}
