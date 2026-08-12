namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanClientCandidateDto(
    string Id,
    string Identification,
    string FullName,
    string Email,
    decimal CurrentDebt);
