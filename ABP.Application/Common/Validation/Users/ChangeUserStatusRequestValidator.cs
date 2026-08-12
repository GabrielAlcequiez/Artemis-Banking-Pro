using ABP.Application.Common.DTOs.Users;
using FluentValidation;

namespace ABP.Application.Common.Validation.Users
{
    public class ChangeUserStatusRequestValidator : AbstractValidator<ChangeUserStatusRequestDto>
    {
        public ChangeUserStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotNull().WithMessage("El campo status es obligatorio.");
        }
    }
}