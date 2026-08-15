using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Lists the authenticated Client's own active savings accounts for MVC option dropdowns.</summary>
    public sealed class ClientAccountOptionsService(
        ISavingsAccountRepository accounts,
        IAccountTransactionRepository transactions,
        ICurrentUserService currentUser,
        IMapper mapper) : IClientAccountOptionsService
    {
        private const int RecentTransactionsCount = 10;

        public async Task<IReadOnlyCollection<SavingsAccountOperationOptionDto>> GetMyActiveAccountsAsync(
            CancellationToken cancellationToken = default)
        {
            var clientId = GetCurrentClientId();

            if (clientId is null)
            {
                return Array.Empty<SavingsAccountOperationOptionDto>();
            }

            var ownedAccounts = await accounts.GetActiveByOwnerIdAsync(
                clientId, cancellationToken);

            return ownedAccounts
                .Select(account => new SavingsAccountOperationOptionDto(
                    account.Id, account.AccountNumber, account.Balance))
                .ToArray();
        }

        public async Task<SavingsAccountDetailDto?> GetDetailAsync(
            Guid accountId, CancellationToken cancellationToken = default)
        {
            var clientId = GetCurrentClientId();

            if (clientId is null)
            {
                return null;
            }

            var account = await accounts.GetByIdAsync(accountId, cancellationToken);

            if (account is null || account.OwnerUserId != clientId)
            {
                return null;
            }

            var recent = await transactions.GetMostRecentByAccountAsync(
                account.Id, RecentTransactionsCount, cancellationToken);

            var dto = mapper.Map<SavingsAccountDetailDto>(account);
            dto.RecentTransactions = mapper.Map<IReadOnlyCollection<AccountTransactionDto>>(recent);

            return dto;
        }

        private string? GetCurrentClientId() =>
            currentUser.IsAuthenticated &&
            currentUser.IsInRole(nameof(Roles.Client)) &&
            !string.IsNullOrWhiteSpace(currentUser.UserId)
                ? currentUser.UserId
                : null;
    }
}
