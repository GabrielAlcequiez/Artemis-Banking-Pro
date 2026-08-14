using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Lists the authenticated Client's own active savings accounts for MVC option dropdowns.</summary>
    public sealed class ClientAccountOptionsService(
        ISavingsAccountRepository accounts,
        ICurrentUserService currentUser) : IClientAccountOptionsService
    {
        public async Task<IReadOnlyCollection<SavingsAccountOperationOptionDto>> GetMyActiveAccountsAsync(
            CancellationToken cancellationToken = default)
        {
            if (!currentUser.IsAuthenticated ||
                !currentUser.IsInRole(nameof(Roles.Client)) ||
                string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Array.Empty<SavingsAccountOperationOptionDto>();
            }

            var ownedAccounts = await accounts.GetActiveByOwnerIdAsync(
                currentUser.UserId, cancellationToken);

            return ownedAccounts
                .Select(account => new SavingsAccountOperationOptionDto(
                    account.Id, account.AccountNumber, account.Balance))
                .ToArray();
        }
    }
}
