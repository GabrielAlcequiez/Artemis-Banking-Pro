using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.Deposit;

public sealed class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator(IValidator<DepositRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
