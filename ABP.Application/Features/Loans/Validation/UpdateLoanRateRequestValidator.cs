using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Validation
{
    public sealed class UpdateLoanRateRequestValidator : AbstractValidator<UpdateLoanRateRequest>
    {
        public UpdateLoanRateRequestValidator()
        {
            RuleFor(request => request.LoanId)
                .NotEmpty()
                .WithMessage("El identificador del préstamo es obligatorio.");

            RuleFor(request => request.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("La tasa de interés anual no puede ser negativa.")
                .PrecisionScale(9, 4, true)
                .WithMessage("La tasa de interés anual debe tener un máximo de cuatro decimales.");
        }
    }
}
