using ABP.Application.Features.Accounts.DTOs;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface IClientAccountOptionsService
{
    Task<IReadOnlyCollection<SavingsAccountOperationOptionDto>> GetMyActiveAccountsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns the account's detail only when it belongs to the authenticated Client; otherwise null.</summary>
    Task<SavingsAccountDetailDto?> GetDetailAsync(
        Guid accountId, CancellationToken cancellationToken = default);
}
