using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token de restablecimiento es obligatorio.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");
        }
    }
}
