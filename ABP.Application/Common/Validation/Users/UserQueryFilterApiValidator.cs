using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class UserQueryFilterApiValidator : AbstractValidator<UserQueryFilterDto>
    {
        private static readonly string[] AllowedRoles = ["administrador", "cajero", "cliente"];

        public UserQueryFilterApiValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(20).WithMessage("El parámetro pageSize no puede superar 20.");

            RuleFor(x => x.Role)
                .Must(role => string.IsNullOrWhiteSpace(role) || AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                .WithMessage("El parámetro role solo puede ser administrador, cajero o cliente.")
                .When(x => !string.IsNullOrWhiteSpace(x.Role));
        }
    }
}