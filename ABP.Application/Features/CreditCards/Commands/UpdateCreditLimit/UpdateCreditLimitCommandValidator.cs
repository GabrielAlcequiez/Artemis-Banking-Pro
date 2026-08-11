using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;

public sealed class UpdateCreditLimitCommandValidator
    : AbstractValidator<UpdateCreditLimitCommand>
{
    public UpdateCreditLimitCommandValidator(
        IValidator<UpdateCreditLimitRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
