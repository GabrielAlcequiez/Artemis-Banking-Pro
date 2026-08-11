using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetSavingsAccounts;

public sealed record GetSavingsAccountsQuery(
    PagedRequest PagedRequest,
    string? OwnerIdentification,
    SavingsAccountStatus? Status,
    SavingsAccountType? Type) : IRequest<PagedResult<SavingsAccountSummaryDto>>;
