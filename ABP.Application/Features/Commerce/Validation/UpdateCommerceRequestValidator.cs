using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Validation;

public sealed class UpdateCommerceRequestValidator : CommerceDataValidator<UpdateCommerceRequest>
{
    public UpdateCommerceRequestValidator()
    {
        RuleFor(request => request.CommerceId)
            .NotEmpty()
            .WithMessage("El identificador del comercio es requerido.");

        AddCommerceDataRules(
            request => request.Name,
            request => request.Description,
            request => request.Email,
            request => request.PhoneNumber,
            request => request.Rnc);
    }
}
