using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.Withdraw;

public sealed record WithdrawCommand(
    WithdrawalRequest Request) : IRequest<OperationResult<FinancialOperationReceipt>>;
