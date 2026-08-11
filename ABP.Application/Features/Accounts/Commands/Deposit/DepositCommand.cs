using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.Deposit;

public sealed record DepositCommand(
    DepositRequest Request) : IRequest<OperationResult<FinancialOperationReceipt>>;
