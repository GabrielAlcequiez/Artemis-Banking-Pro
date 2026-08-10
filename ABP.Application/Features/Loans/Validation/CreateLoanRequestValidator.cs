using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Validation
{
    public sealed class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
    {
        private static readonly int[] AllowedTerms =
            [6, 12, 18, 24, 30, 36, 42, 48, 54, 60];

        public CreateLoanRequestValidator()
        {
            RuleFor(request => request.ClientId)
                .NotEmpty()
                .WithMessage("El identificador del cliente es obligatorio.");

            RuleFor(request => request.CapitalAmount)
                .GreaterThan(0m)
                .WithMessage("El monto del capital debe ser mayor que cero.")
                .PrecisionScale(18, 2, true)
                .WithMessage("El monto del capital debe tener un máximo de dos decimales.");

            RuleFor(request => request.TermInMonths)
                .Must(AllowedTerms.Contains)
                .WithMessage("El plazo debe ser uno de los valores permitidos: 6, 12, 18, 24, 30, 36, 42, 48, 54 o 60 meses.");

            RuleFor(request => request.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("La tasa de interés anual no puede ser negativa.")
                .PrecisionScale(9, 4, true)
                .WithMessage("La tasa de interés anual debe tener un máximo de cuatro decimales.");
        }
    }
}
