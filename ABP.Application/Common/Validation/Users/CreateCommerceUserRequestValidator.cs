using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class CreateCommerceUserRequestValidator : AbstractValidator<CreateCommerceUserRequestDto>
    {
        public CreateCommerceUserRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(50).WithMessage("El apellido no puede exceder 50 caracteres.");

            RuleFor(x => x.Identification)
                .NotEmpty().WithMessage("La cédula o identificador es obligatorio.")
                .MaximumLength(11).WithMessage("La cédula o identificador no puede exceder 11 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico es inválido.")
                .MaximumLength(256).WithMessage("El correo electrónico no puede exceder 256 caracteres.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .MaximumLength(20).WithMessage("El nombre de usuario no puede exceder 20 caracteres.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");

            RuleFor(x => x.InitialAmount)
                .GreaterThanOrEqualTo(0).WithMessage("El monto inicial no puede ser negativo.");
        }
    }
}
