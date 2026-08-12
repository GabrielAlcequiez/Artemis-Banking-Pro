using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class ConfirmAccountRequestValidator : AbstractValidator<ConfirmAccountRequestDto>
    {
        public ConfirmAccountRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token de confirmación es obligatorio.");
        }
    }
}