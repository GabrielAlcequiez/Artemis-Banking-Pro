using FluentValidation;

namespace ABP.Application.Features.Accounts.Queries.GetBeneficiaries;

public sealed class GetBeneficiariesQueryValidator : AbstractValidator<GetBeneficiariesQuery>
{
    public GetBeneficiariesQueryValidator()
    {
        RuleFor(query => query.OwnerUserId)
            .NotEmpty()
            .WithMessage("OwnerUserId is required.");
    }
}
