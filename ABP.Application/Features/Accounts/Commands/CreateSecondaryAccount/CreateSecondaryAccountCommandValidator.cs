using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.CreateSecondaryAccount;

public sealed class CreateSecondaryAccountCommandValidator
    : AbstractValidator<CreateSecondaryAccountCommand>
{
    public CreateSecondaryAccountCommandValidator(
        IValidator<CreateSecondaryAccountRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
