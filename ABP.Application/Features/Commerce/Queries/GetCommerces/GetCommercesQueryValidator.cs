using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Queries.GetCommerces;

public sealed class GetCommercesQueryValidator : AbstractValidator<GetCommercesQuery>
{
    public GetCommercesQueryValidator(IValidator<CommerceListRequest> requestValidator)
    {
        RuleFor(query => query.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
