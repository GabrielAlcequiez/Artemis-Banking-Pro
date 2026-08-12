using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using MediatR;

namespace ABP.Application.Features.Loans.Commands.CreateLoan;

public sealed record CreateLoanCommand(
    CreateLoanRequest Request)
    : IRequest<OperationResult<LoanDetailDto>>;
