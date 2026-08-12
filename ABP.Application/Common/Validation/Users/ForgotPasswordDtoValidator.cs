using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");
        }
    }
}