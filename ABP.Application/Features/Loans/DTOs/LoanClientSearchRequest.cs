namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanClientSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Identification = null);
