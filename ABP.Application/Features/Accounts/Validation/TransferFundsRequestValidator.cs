using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Enums;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Validation
{
    public sealed class TransferFundsRequestValidator : AbstractValidator<TransferFundsRequest>
    {
        private static readonly FinancialOperationType[] AllowedOperationTypes =
        [
            FinancialOperationType.ExpressTransfer,
            FinancialOperationType.BeneficiaryTransfer,
            FinancialOperationType.OwnAccountTransfer
        ];

        public TransferFundsRequestValidator()
        {
            RuleFor(request => request.SourceAccountId)
                .NotEmpty()
                .WithMessage("SourceAccountId is required.");

            RuleFor(request => request.Amount)
                .GreaterThan(0m)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(request => request.OperationType)
                .Must(type => AllowedOperationTypes.Contains(type))
                .WithMessage("OperationType must be ExpressTransfer, BeneficiaryTransfer or OwnAccountTransfer.");

            RuleFor(request => request)
                .Must(request =>
                    !string.IsNullOrWhiteSpace(request.DestinationAccountNumber) ||
                    request.DestinationAccountId is not null)
                .WithMessage("Either DestinationAccountNumber or DestinationAccountId is required.");

            RuleFor(request => request.ActorUserId)
                .NotEmpty()
                .WithMessage("ActorUserId is required.");

            RuleFor(request => request.ActorRole)
                .NotEmpty()
                .WithMessage("ActorRole is required.");
        }
    }
}
