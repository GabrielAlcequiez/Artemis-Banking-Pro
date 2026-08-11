using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Validation
{
    public sealed class AddBeneficiaryRequestValidator : AbstractValidator<AddBeneficiaryRequest>
    {
        public AddBeneficiaryRequestValidator()
        {
            RuleFor(request => request.OwnerUserId)
                .NotEmpty()
                .WithMessage("OwnerUserId is required.");

            RuleFor(request => request.BeneficiaryAccountNumber)
                .NotEmpty()
                .Length(9)
                .WithMessage("BeneficiaryAccountNumber must be a 9-digit account number.");
        }
    }
}
