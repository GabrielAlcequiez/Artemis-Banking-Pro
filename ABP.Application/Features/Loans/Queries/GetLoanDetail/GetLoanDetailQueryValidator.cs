using FluentValidation;

namespace ABP.Application.Features.Loans.Queries.GetLoanDetail;

public sealed class GetLoanDetailQueryValidator : AbstractValidator<GetLoanDetailQuery>
{
    public GetLoanDetailQueryValidator()
    {
        RuleFor(query => query.LoanId)
            .NotEmpty()
            .WithMessage("El identificador del préstamo es obligatorio.");
    }
}
