using ABP.Application.Features.Loans.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Loans.Validation;

public sealed class LoanClientSearchRequestValidator
    : AbstractValidator<LoanClientSearchRequest>
{
    public LoanClientSearchRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThan(0)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("La cantidad de registros por página debe estar entre 1 y 20.");

        RuleFor(request => request.Identification)
            .Must(value => value is null || value.Trim().Length <= 11)
            .WithMessage("La cédula debe contener como máximo 11 caracteres.");
    }
}
