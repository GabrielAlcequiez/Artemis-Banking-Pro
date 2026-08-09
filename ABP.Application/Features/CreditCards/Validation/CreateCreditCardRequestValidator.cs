using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;
namespace ABP.Application.Features.CreditCards.Validation
{
    public sealed class CreateCreditCardRequestValidator : AbstractValidator<CreateCreditCardRequest>
    {
        public CreateCreditCardRequestValidator()
        {
            RuleFor(request => request.ClientId)
                .NotEmpty()
                .WithMessage("ClientId is required.");

            RuleFor(request => request.CreditLimit)
                .GreaterThan(0m)
                .WithMessage("CreditLimit must be greater than zero.");
        }
    }
}