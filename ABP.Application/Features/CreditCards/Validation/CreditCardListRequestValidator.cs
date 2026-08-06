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
            .WithMessage("Page must be greater than zero.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("PageSize must be between 1 and 20.");

        RuleFor(request => request.Identification)
            .Must(HasValidLengthAfterTrim)
            .WithMessage("Identification must contain at most 11 characters after trimming.");

        RuleFor(request => request.Status)
            .Must(status => !status.HasValue
                || Enum.IsDefined(typeof(CreditCardStatusFilter), status.Value))
            .WithMessage("Status must be null or a defined credit card status.");
    }

    private static bool HasValidLengthAfterTrim(string? identification) =>
        identification is null || identification.Trim().Length <= 11;
}
