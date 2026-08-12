using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Read-only access to savings accounts for consumers that don't go through MediatR (Admin MVC).</summary>
    public sealed class SavingsAccountQueryService(
        ISavingsAccountRepository accounts,
        IAccountTransactionRepository transactions,
        IMapper mapper) : ISavingsAccountQueryService
    {
        private const int RecentTransactionsCount = 10;

        public async Task<PagedResult<SavingsAccountSummaryDto>> ListAsync(
            PagedRequest pagedRequest,
            string? ownerIdentification,
            SavingsAccountStatus? status,
            SavingsAccountType? type,
            CancellationToken cancellationToken = default)
        {
            var page = await accounts.GetPagedAsync(
                pagedRequest, ownerIdentification, status, type, cancellationToken);

            var data = mapper.Map<IReadOnlyCollection<SavingsAccountSummaryDto>>(page.Data);

            return new PagedResult<SavingsAccountSummaryDto>(data, page.Page, page.PageSize, page.TotalRecords);
        }

        public async Task<SavingsAccountDetailDto?> GetDetailAsync(
            Guid accountId, CancellationToken cancellationToken = default)
        {
            var account = await accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                return null;
            }

            var recent = await transactions.GetMostRecentByAccountAsync(
                account.Id, RecentTransactionsCount, cancellationToken);

            var dto = mapper.Map<SavingsAccountDetailDto>(account);
            dto.RecentTransactions = mapper.Map<IReadOnlyCollection<AccountTransactionDto>>(recent);

            return dto;
        }
    }
}
