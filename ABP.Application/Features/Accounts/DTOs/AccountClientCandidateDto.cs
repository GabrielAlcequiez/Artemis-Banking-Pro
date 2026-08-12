namespace ABP.Application.Features.Accounts.DTOs;

public sealed record AccountClientCandidateDto(
    string Id,
    string Identification,
    string FullName,
    string Email);
