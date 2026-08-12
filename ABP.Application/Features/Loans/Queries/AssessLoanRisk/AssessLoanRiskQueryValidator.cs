using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Queries.AssessLoanRisk;

public sealed class AssessLoanRiskQueryValidator
    : AbstractValidator<AssessLoanRiskQuery>
{
    public AssessLoanRiskQueryValidator(
        IValidator<CreateLoanRequest> requestValidator)
    {
        RuleFor(query => query.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
