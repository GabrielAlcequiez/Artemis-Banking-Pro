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
                .WithMessage("El límite de crédito debe ser mayor que cero.")
                .PrecisionScale(18, 2, true)
                .WithMessage("El límite de crédito debe tener un máximo de dos decimales.");

            RuleFor(request => request.OperationId)
                .NotEmpty()
                .WithMessage("El identificador de la operación es requerido.");
        }
    }
}
