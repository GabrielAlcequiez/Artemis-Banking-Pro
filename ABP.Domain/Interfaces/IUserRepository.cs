using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;

namespace ABP.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User, string>
    {
        Task<User?> FindByIdentificationAsync(string identification);

        Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default);
    }
}