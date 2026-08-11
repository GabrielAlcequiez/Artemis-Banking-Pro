using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.TransferFunds;

public sealed record TransferFundsCommand(
    TransferFundsRequest Request) : IRequest<OperationResult<FinancialOperationReceipt>>;
