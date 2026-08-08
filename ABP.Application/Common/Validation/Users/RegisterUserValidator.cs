using System.Linq;
using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class RegisterUserValidator : AbstractValidator<CreateUserDto>
    {
        private static readonly string[] ValidRoles = ["Administrador", "Cajero", "Cliente"];

        public RegisterUserValidator()
        {
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

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("El rol es obligatorio.")
                .Must(role => ValidRoles.Contains(role))
                .WithMessage("El rol especificado no es válido. Debe ser Administrador, Cajero o Cliente.");

            RuleFor(x => x.InitialBalance)
                .GreaterThanOrEqualTo(0).When(x => x.InitialBalance.HasValue)
                .WithMessage("El monto inicial no puede ser negativo.");
        }
    }
}
