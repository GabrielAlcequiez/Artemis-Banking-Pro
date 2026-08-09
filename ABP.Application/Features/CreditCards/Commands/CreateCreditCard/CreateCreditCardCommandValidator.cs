using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardCommandValidator
    : AbstractValidator<CreateCreditCardCommand>
{
    public CreateCreditCardCommandValidator(
        IValidator<CreateCreditCardRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
