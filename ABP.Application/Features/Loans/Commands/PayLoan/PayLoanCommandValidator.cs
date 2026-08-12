using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Commands.PayLoan;

public sealed class PayLoanCommandValidator
    : AbstractValidator<PayLoanCommand>
{
    public PayLoanCommandValidator(
        IValidator<LoanPaymentRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
