using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.TransferFunds;

public sealed class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator(IValidator<TransferFundsRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
