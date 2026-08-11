using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation;

public sealed class CreditCardClientSearchRequestValidator
    : AbstractValidator<CreditCardClientSearchRequest>
{
    public CreditCardClientSearchRequestValidator()
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
