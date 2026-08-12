using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Queries.GetLoans;

public sealed class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
{
    public GetLoansQueryValidator(
        IValidator<LoanListRequest> requestValidator)
    {
        RuleFor(query => query.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
