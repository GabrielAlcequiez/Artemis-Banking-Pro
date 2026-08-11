using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetSavingsAccountDetail;

public sealed class GetSavingsAccountDetailQueryHandler(
    ISavingsAccountRepository accounts,
    IAccountTransactionRepository transactions,
    IMapper mapper) : IRequestHandler<GetSavingsAccountDetailQuery, SavingsAccountDetailDto?>
{
    private const int RecentTransactionsCount = 10;

    public async Task<SavingsAccountDetailDto?> Handle(
        GetSavingsAccountDetailQuery query, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(query.AccountId, cancellationToken);
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
