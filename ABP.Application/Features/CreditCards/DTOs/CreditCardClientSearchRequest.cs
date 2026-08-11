namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardClientSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Identification = null);
