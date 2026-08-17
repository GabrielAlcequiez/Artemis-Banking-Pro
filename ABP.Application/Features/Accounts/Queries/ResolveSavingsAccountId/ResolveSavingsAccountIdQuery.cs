using MediatR;

namespace ABP.Application.Features.Accounts.Queries.ResolveSavingsAccountId;

/// <summary>Looks up a savings account's internal id by its public 9-digit account number.</summary>
public sealed record ResolveSavingsAccountIdQuery(string AccountNumber) : IRequest<Guid?>;
