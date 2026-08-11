using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetBeneficiaries;

public sealed class GetBeneficiariesQueryHandler(IBeneficiaryService beneficiaries)
    : IRequestHandler<GetBeneficiariesQuery, IReadOnlyCollection<BeneficiaryDto>>
{
    public Task<IReadOnlyCollection<BeneficiaryDto>> Handle(
        GetBeneficiariesQuery query, CancellationToken cancellationToken)
    {
        return beneficiaries.ListAsync(query.OwnerUserId, cancellationToken);
    }
}
