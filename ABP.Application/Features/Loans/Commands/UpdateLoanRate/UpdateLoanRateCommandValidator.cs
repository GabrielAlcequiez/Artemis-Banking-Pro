using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Commands.UpdateLoanRate;

public sealed class UpdateLoanRateCommandValidator
    : AbstractValidator<UpdateLoanRateCommand>
{
    public UpdateLoanRateCommandValidator(
        IValidator<UpdateLoanRateRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
