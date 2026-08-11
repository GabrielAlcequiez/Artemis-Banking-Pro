using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Validation;

public sealed class ChangeCommerceStatusRequestValidator : AbstractValidator<ChangeCommerceStatusRequest>
{
    public ChangeCommerceStatusRequestValidator()
    {
        RuleFor(request => request.CommerceId)
            .NotEmpty()
            .WithMessage("El identificador del comercio es requerido.");
    }
}
