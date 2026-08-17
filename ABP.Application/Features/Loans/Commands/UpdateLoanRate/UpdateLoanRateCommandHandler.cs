using ABP.Application.Common;
using ABP.Application.Features.Loans.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.UpdateLoanRate;

public sealed class UpdateLoanRateCommandHandler(
    ILoanRateService loanRateService)
    : IRequestHandler<UpdateLoanRateCommand, OperationResult>
{
    public async Task<OperationResult> Handle(
        UpdateLoanRateCommand command,
        CancellationToken cancellationToken)
    {
        var result = await loanRateService.UpdateRateAsync(
            command.Request,
            cancellationToken);

        return result.Operation;
    }
}
