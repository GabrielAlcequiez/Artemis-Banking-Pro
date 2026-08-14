using ABP.Application.Features.Accounts.DTOs;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface IClientAccountOptionsService
{
    Task<IReadOnlyCollection<SavingsAccountOperationOptionDto>> GetMyActiveAccountsAsync(
        CancellationToken cancellationToken = default);
}
