using ABP.Application.Features.HermesPay.DTOs;
using FluentValidation;

namespace ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;

public sealed class ProcessHermesPaymentCommandValidator
    : AbstractValidator<ProcessHermesPaymentCommand>
{
    public ProcessHermesPaymentCommandValidator(
        IValidator<ProcessHermesPaymentRequest> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
