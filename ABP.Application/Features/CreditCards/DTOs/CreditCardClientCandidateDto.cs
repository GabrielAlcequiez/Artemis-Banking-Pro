namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardClientCandidateDto(
    string Id,
    string Identification,
    string FullName,
    string Email,
    decimal TotalDebt);
