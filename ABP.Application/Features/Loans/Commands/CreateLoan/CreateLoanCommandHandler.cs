using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.CreateLoan;

public sealed class CreateLoanCommandHandler(
    ILoanOriginationService originationService)
    : IRequestHandler<CreateLoanCommand, OperationResult<LoanDetailDto>>
{
    public Task<OperationResult<LoanDetailDto>> Handle(
        CreateLoanCommand command,
        CancellationToken cancellationToken)
    {
        return originationService.CreateAsync(
            command.Request,
            cancellationToken);
    }
}
