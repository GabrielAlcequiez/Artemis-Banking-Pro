using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Enums;
using FluentValidation;

namespace ABP.Application.Features.Loans.Validation;

public sealed class LoanListRequestValidator : AbstractValidator<LoanListRequest>
{
    public LoanListRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThan(0)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("El tamaño de página debe estar entre 1 y 20.");

        RuleFor(request => request.Identification)
            .Must(HasValidLengthAfterTrim)
            .WithMessage("La cédula debe contener un máximo de 11 caracteres.");

        RuleFor(request => request.Status)
            .Must(status => !status.HasValue
                || Enum.IsDefined(typeof(LoanStatusFilter), status.Value))
            .WithMessage("El estado del préstamo no es válido.");
    }

    private static bool HasValidLengthAfterTrim(string? identification) =>
        identification is null || identification.Trim().Length <= 11;
}
