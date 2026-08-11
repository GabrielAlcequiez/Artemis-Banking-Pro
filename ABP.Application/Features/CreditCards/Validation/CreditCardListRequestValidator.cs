using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Enums;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Validation;

public sealed class CreditCardListRequestValidator : AbstractValidator<CreditCardListRequest>
{
    public CreditCardListRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThan(0)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("La cantidad de registros por página debe estar entre 1 y 20.");

        RuleFor(request => request.Identification)
            .Must(HasValidLengthAfterTrim)
            .WithMessage("La cédula debe contener como máximo 11 caracteres.");

        RuleFor(request => request.Status)
            .Must(status => !status.HasValue
                || Enum.IsDefined(typeof(CreditCardStatusFilter), status.Value))
            .WithMessage("El estado de la tarjeta no es válido.");
    }

    private static bool HasValidLengthAfterTrim(string? identification) =>
        identification is null || identification.Trim().Length <= 11;
}
