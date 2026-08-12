using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Commands.CreateLoan;

public sealed class CreateLoanCommandValidator
    : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator(
        IValidator<CreateLoanRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
