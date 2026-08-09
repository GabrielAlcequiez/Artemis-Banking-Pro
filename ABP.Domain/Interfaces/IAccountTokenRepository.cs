using ABP.Domain.Entities;
using ABP.Domain.Enums;

namespace ABP.Domain.Interfaces
{
    public interface IAccountTokenRepository
    {
        Task<AccountToken?> ExistsAsync(string userId, AccountTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default);
        Task<int> MarkAsUsedAsync(Guid accountTokenId, DateTimeOffset dateTimeOffset, CancellationToken cancellationToken = default);
    }
}