namespace ABP.Application.Features.Accounts.DTOs;

public sealed record AccountClientSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Identification = null);
