using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetSavingsAccountDetail;

public sealed record GetSavingsAccountDetailQuery(
    Guid AccountId) : IRequest<SavingsAccountDetailDto?>;
