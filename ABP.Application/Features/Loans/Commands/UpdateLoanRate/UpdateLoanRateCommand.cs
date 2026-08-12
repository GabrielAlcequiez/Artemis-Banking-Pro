using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.UpdateLoanRate;

public sealed record UpdateLoanRateCommand(
    UpdateLoanRateRequest Request) : IRequest<OperationResult>;
