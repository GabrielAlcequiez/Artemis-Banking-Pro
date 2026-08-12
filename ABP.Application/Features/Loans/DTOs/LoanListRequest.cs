using ABP.Domain.Enums;

namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Identification = null,
    LoanStatusFilter? Status = null);
