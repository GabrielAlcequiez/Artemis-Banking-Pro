using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation;

public sealed class CreditCardPaymentRequestValidator
    : AbstractValidator<CreditCardPaymentRequest>
{
    public CreditCardPaymentRequestValidator()
    {
        RuleFor(request => request.CreditCardId)
            .NotEmpty()
            .WithMessage("La tarjeta de crédito destino es requerida.");

        RuleFor(request => request.SourceAccountId)
            .NotEmpty()
            .WithMessage("La cuenta de origen es requerida.");

        RuleFor(request => request.Amount)
            .GreaterThan(0m)
            .WithMessage("El monto a pagar debe ser mayor que cero.")
            .PrecisionScale(18, 2, true)
            .WithMessage("El monto a pagar debe tener un máximo de dos decimales.");

        RuleFor(request => request.OperationId)
            .NotEmpty()
            .WithMessage("El identificador de la operación es requerido.");
    }
}
