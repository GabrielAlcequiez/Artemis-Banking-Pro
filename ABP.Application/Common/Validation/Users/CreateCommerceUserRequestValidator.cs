using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class CreateCommerceUserRequestValidator : AbstractValidator<CreateCommerceUserRequestDto>
    {
        public CreateCommerceUserRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.");

            RuleFor(x => x.Identification)
                .NotEmpty().WithMessage("La cédula o identificador es obligatorio.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico es inválido.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

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