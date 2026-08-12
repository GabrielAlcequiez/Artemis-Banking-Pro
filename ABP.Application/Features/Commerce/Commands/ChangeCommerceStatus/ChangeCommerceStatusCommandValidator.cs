using ABP.Application.Features.Commerce.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;

public sealed class ChangeCommerceStatusCommandValidator
    : AbstractValidator<ChangeCommerceStatusCommand>
{
    public ChangeCommerceStatusCommandValidator(
        IValidator<ChangeCommerceStatusRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
