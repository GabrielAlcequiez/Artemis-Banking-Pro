using ABP.Application.Common;
using ABP.Application.Features.HermesPay.DTOs;
using MediatR;

namespace ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;

public sealed record ProcessHermesPaymentCommand(
    ProcessHermesPaymentRequest Request)
    : IRequest<OperationResult<FinancialOperationReceipt>>;
