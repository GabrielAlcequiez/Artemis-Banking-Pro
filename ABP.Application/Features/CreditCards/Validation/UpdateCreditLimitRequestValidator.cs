using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation
{
    public sealed class UpdateCreditLimitRequestValidator : AbstractValidator<UpdateCreditLimitRequest>
    {
        public UpdateCreditLimitRequestValidator()
        {
            RuleFor(request => request.CreditCardId)
                .NotEmpty()
                .WithMessage("CreditCardId is required.");

            RuleFor(request => request.CreditLimit)
                .GreaterThan(0m)
                .WithMessage("CreditLimit must be greater than zero.");
        }
    }
}