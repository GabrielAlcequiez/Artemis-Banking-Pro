using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetBeneficiaries;

public sealed record GetBeneficiariesQuery(
    string OwnerUserId) : IRequest<IReadOnlyCollection<BeneficiaryDto>>;
