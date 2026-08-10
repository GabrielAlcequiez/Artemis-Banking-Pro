using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Validation
{
    public sealed class LoanPaymentRequestValidator : AbstractValidator<LoanPaymentRequest>
    {
        public LoanPaymentRequestValidator()
        {
            RuleFor(request => request.LoanId)
                .NotEmpty()
                .WithMessage("El identificador del préstamo es obligatorio.");

            RuleFor(request => request.SourceAccountId)
                .NotEmpty()
                .WithMessage("El identificador de la cuenta de origen es obligatorio.");

            RuleFor(request => request.Amount)
                .GreaterThan(0m)
                .WithMessage("El monto del pago debe ser mayor que cero.")
                .PrecisionScale(18, 2, true)
                .WithMessage("El monto del pago debe tener un máximo de dos decimales.");

            RuleFor(request => request.OperationId)
                .NotEmpty()
                .WithMessage("El identificador de la operación es obligatorio.");
        }
    }
}
