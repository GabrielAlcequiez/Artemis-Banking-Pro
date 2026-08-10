using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Commands.CancelCreditCard;

public sealed class CancelCreditCardCommandValidator
    : AbstractValidator<CancelCreditCardCommand>
{
    public CancelCreditCardCommandValidator(
        IValidator<CancelCreditCardRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
