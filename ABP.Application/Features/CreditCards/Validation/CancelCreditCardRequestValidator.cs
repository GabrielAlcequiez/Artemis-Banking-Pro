using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation
{
    public sealed class CancelCreditCardRequestValidator : AbstractValidator<CancelCreditCardRequest>
    {
        public CancelCreditCardRequestValidator()
        {
            RuleFor(request => request.CreditCardId)
                .NotEmpty()
                .WithMessage("CreditCardId is required.");
        }
    }
}