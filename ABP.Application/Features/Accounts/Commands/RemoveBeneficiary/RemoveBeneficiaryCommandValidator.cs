using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.RemoveBeneficiary;

public sealed class RemoveBeneficiaryCommandValidator : AbstractValidator<RemoveBeneficiaryCommand>
{
    public RemoveBeneficiaryCommandValidator()
    {
        RuleFor(command => command.OwnerUserId)
            .NotEmpty()
            .WithMessage("OwnerUserId is required.");

        RuleFor(command => command.BeneficiaryId)
            .NotEmpty()
            .WithMessage("BeneficiaryId is required.");
    }
}
