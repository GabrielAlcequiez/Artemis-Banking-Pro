using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Commands.UpdateCommerce;

public sealed class UpdateCommerceCommandValidator
    : AbstractValidator<UpdateCommerceCommand>
{
    public UpdateCommerceCommandValidator(
        IValidator<UpdateCommerceRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
