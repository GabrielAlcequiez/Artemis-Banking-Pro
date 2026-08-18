using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation;

public sealed class CashAdvanceRequestValidator
    : AbstractValidator<CashAdvanceRequest>
{
    public CashAdvanceRequestValidator()
    {
        RuleFor(request => request.CreditCardId)
            .NotEmpty()
            .WithMessage("La tarjeta de crédito origen es requerida.");

        RuleFor(request => request.TargetAccountId)
            .NotEmpty()
            .WithMessage("La cuenta de ahorro destino es requerida.");

        RuleFor(request => request.Amount)
            .GreaterThan(0m)
            .WithMessage("El monto del avance debe ser mayor que cero.")
            .PrecisionScale(18, 2, true)
            .WithMessage("El monto del avance debe tener un máximo de dos decimales.");

        RuleFor(request => request.OperationId)
            .NotEmpty()
            .WithMessage("El identificador de la operación es requerido.");
    }
}
