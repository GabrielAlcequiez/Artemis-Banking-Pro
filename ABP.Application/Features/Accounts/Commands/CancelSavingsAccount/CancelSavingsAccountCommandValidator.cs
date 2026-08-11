using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.CancelSavingsAccount;

public sealed class CancelSavingsAccountCommandValidator
    : AbstractValidator<CancelSavingsAccountCommand>
{
    public CancelSavingsAccountCommandValidator(
        IValidator<CancelSavingsAccountRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
