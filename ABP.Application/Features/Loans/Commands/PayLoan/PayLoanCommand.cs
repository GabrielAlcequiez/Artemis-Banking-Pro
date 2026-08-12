using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.PayLoan;

public sealed record PayLoanCommand(
    LoanPaymentRequest Request)
    : IRequest<OperationResult<LoanPaymentResult>>;
