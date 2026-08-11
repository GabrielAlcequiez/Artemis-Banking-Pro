using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Validation
{
    public sealed class WithdrawalRequestValidator : AbstractValidator<WithdrawalRequest>
    {
        public WithdrawalRequestValidator()
        {
            RuleFor(request => request.SourceAccountNumber)
                .NotEmpty()
                .Length(9)
                .WithMessage("SourceAccountNumber must be a 9-digit account number.");

            RuleFor(request => request.Amount)
                .GreaterThan(0m)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(request => request.ActorUserId)
                .NotEmpty()
                .WithMessage("ActorUserId is required.");

            RuleFor(request => request.ActorRole)
                .NotEmpty()
                .WithMessage("ActorRole is required.");
        }
    }
}
