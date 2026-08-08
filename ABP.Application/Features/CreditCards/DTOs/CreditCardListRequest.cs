using ABP.Domain.Enums;

namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Identification = null,
    CreditCardStatusFilter? Status = null);