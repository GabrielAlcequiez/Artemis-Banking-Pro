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
                .WithMessage("El cliente seleccionado es requerido.");

            RuleFor(request => request.CreditLimit)
                .GreaterThan(0m)
                .WithMessage("El límite de crédito debe ser mayor que cero.");
        }
    }
}
