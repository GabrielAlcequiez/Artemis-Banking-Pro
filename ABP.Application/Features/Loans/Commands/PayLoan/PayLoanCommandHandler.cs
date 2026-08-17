using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.PayLoan;

public sealed class PayLoanCommandHandler(
    ILoanPaymentService paymentService)
    : IRequestHandler<PayLoanCommand, OperationResult<LoanPaymentResult>>
{
    public async Task<OperationResult<LoanPaymentResult>> Handle(
        PayLoanCommand command,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessPaymentAsync(
            command.Request,
            cancellationToken);

        return result.Operation;
    }
}
