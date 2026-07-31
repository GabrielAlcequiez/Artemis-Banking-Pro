using ABP.Domain.Entities.Commerce;

namespace ABP.Application.Interfaces.Persistence
{
    public interface ICommerceRepository : IGenericRepository<Commerce, Guid>
    {
        Task<bool> EmailExistsAsync(string email, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default);
        Task<bool> RncExistsAsync(string rnc, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default);

    }
}