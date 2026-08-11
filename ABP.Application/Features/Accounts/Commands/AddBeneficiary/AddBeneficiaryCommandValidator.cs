using ABP.Application.Features.Accounts.DTOs;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Commands.AddBeneficiary;

public sealed class AddBeneficiaryCommandValidator : AbstractValidator<AddBeneficiaryCommand>
{
    public AddBeneficiaryCommandValidator(IValidator<AddBeneficiaryRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
