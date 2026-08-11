using System.Linq.Expressions;
using FluentValidation;

namespace ABP.Application.Features.Commerce.Validation;

public abstract class CommerceDataValidator<T> : AbstractValidator<T>
{
    protected void AddCommerceDataRules(
        Expression<Func<T, string>> name,
        Expression<Func<T, string?>> description,
        Expression<Func<T, string>> email,
        Expression<Func<T, string>> phoneNumber,
        Expression<Func<T, string>> rnc)
    {
        RuleFor(name)
            .NotEmpty()
            .WithMessage("El nombre del comercio es requerido.")
            .MaximumLength(150)
            .WithMessage("El nombre del comercio no puede exceder 150 caracteres.");

        RuleFor(description)
            .MaximumLength(500)
            .WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(email)
            .NotEmpty()
            .WithMessage("El correo electrónico es requerido.")
            .EmailAddress()
            .WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(256)
            .WithMessage("El correo electrónico no puede exceder 256 caracteres.");

        RuleFor(phoneNumber)
            .NotEmpty()
            .WithMessage("El teléfono del comercio es requerido.")
            .MaximumLength(20)
            .WithMessage("El teléfono del comercio no puede exceder 20 caracteres.");

        RuleFor(rnc)
            .NotEmpty()
            .WithMessage("El RNC del comercio es requerido.")
            .MaximumLength(11)
            .WithMessage("El RNC del comercio no puede exceder 11 caracteres.");
    }
}
