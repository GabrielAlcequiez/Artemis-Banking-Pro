using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Validation
{
    public sealed class CreateSecondaryAccountRequestValidator : AbstractValidator<CreateSecondaryAccountRequest>
    {
        public CreateSecondaryAccountRequestValidator()
        {
            RuleFor(request => request.OwnerUserId)
                .NotEmpty()
                .WithMessage("OwnerUserId is required.");

            RuleFor(request => request.InitialBalance)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("InitialBalance cannot be negative.");

            RuleFor(request => request.ActorUserId)
                .NotEmpty()
                .WithMessage("ActorUserId is required.");

            RuleFor(request => request.ActorRole)
                .NotEmpty()
                .WithMessage("ActorRole is required.");
        }
    }
}
