using ABP.Application.Features.Commerce.DTOs;
using ABP.Domain.Enums;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Validation;

public sealed class CommerceListRequestValidator : AbstractValidator<CommerceListRequest>
{
    public CommerceListRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThan(0)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("La cantidad de registros por página debe estar entre 1 y 20.");

        RuleFor(request => request.Status)
            .Must(status => !status.HasValue
                || Enum.IsDefined(typeof(CommerceStatusFilter), status.Value))
            .WithMessage("El estado del comercio no es válido.");
    }
}
