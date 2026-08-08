using ABP.Domain.Common;

namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardListResult(
    PagedResult<CreditCardSummaryDto> Page,
    CreditCardSearchStatus SearchStatus);