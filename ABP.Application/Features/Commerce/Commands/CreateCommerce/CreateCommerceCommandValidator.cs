using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Commands.CreateCommerce;

public sealed class CreateCommerceCommandValidator
    : AbstractValidator<CreateCommerceCommand>
{
    public CreateCommerceCommandValidator(
        IValidator<CreateCommerceRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
