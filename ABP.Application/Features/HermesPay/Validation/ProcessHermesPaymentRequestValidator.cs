using ABP.Application.Features.HermesPay.DTOs;
using FluentValidation;

namespace ABP.Application.Features.HermesPay.Validation;

public sealed class ProcessHermesPaymentRequestValidator
    : AbstractValidator<ProcessHermesPaymentRequest>
{
    public ProcessHermesPaymentRequestValidator()
    {
        RuleFor(request => request.CardNumber)
            .NotEmpty()
            .Matches("^[0-9]{16}$")
            .WithMessage("El número de tarjeta debe contener exactamente 16 dígitos.");

        RuleFor(request => request.ExpirationMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes de expiración debe estar entre 01 y 12.");

        RuleFor(request => request.ExpirationYear)
            .InclusiveBetween(1000, 9999)
            .WithMessage("El año de expiración debe contener cuatro dígitos.");

        RuleFor(request => request.Cvc)
            .NotEmpty()
            .Matches("^[0-9]{3}$")
            .WithMessage("El CVC debe contener exactamente 3 dígitos.");

        RuleFor(request => request.TransactionAmount)
            .GreaterThan(0m)
            .WithMessage("El monto de la transacción debe ser mayor que cero.");

        RuleFor(request => request.OperationId)
            .NotEmpty()
            .WithMessage("El encabezado Idempotency-Key es requerido y debe ser un GUID válido.");
    }
}
