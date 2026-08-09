using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class UpdateUserValidator : AbstractValidator<EditUserDto>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.");

            RuleFor(x => x.Identification)
                .NotEmpty().WithMessage("La cédula es obligatoria.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico es inválido.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

            When(x => !string.IsNullOrEmpty(x.Password), () =>
            {
                RuleFor(x => x.ConfirmPassword)
                    .NotEmpty().WithMessage("Debe confirmar la nueva contraseña.")
                    .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");
            });

            RuleFor(x => x.AdditionalAmount)
                .GreaterThanOrEqualTo(0).When(x => x.AdditionalAmount.HasValue)
                .WithMessage("El monto adicional no puede ser negativo.");
        }
    }
}
