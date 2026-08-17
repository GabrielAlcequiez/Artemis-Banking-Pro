using ABP.Domain.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.ResolveSavingsAccountId;

public sealed class ResolveSavingsAccountIdQueryHandler(ISavingsAccountRepository accounts)
    : IRequestHandler<ResolveSavingsAccountIdQuery, Guid?>
{
    public async Task<Guid?> Handle(ResolveSavingsAccountIdQuery query, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByAccountNumberAsync(query.AccountNumber, cancellationToken);
        return account?.Id;
    }
}
