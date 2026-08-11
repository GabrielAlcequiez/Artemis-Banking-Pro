using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Validation
{
    public sealed class CancelSavingsAccountRequestValidator : AbstractValidator<CancelSavingsAccountRequest>
    {
        public CancelSavingsAccountRequestValidator()
        {
            RuleFor(request => request.AccountId)
                .NotEmpty()
                .WithMessage("AccountId is required.");

            RuleFor(request => request.ActorUserId)
                .NotEmpty()
                .WithMessage("ActorUserId is required.");

            RuleFor(request => request.ActorRole)
                .NotEmpty()
                .WithMessage("ActorRole is required.");
        }
    }
}
