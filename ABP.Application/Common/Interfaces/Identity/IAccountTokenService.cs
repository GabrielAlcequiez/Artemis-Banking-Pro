using ABP.Domain.Enums;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IAccountTokenService
    {
        Task<string> GenerateAsync(string userId, AccountTokenPurpose purpose, CancellationToken cancellationToken = default);

        Task<AccountTokenValidationResult> ValidateAsync(string userId, string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default);

        Task<AccountTokenValidationResult> ValidateByTokenAsync(string token, AccountTokenPurpose purpose, CancellationToken cancellationToken = default);

        Task<bool> TryMarkAsUsedAsync(Guid accountTokenId, CancellationToken cancellationToken = default);
    }

    public enum AccountTokenValidationStatus
    {
        Valid = 1,
        NotFound = 2,
        Used = 3,
        Expired = 4,
        Invalid = 5
    }

    public sealed record AccountTokenValidationResult(
        AccountTokenValidationStatus Status,
        Guid? AccountTokenId = null,
        string? UserId = null);
}
