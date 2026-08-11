using ABP.Domain.Common;

namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardClientSelectionResult(
    PagedResult<CreditCardClientCandidateDto> Page,
    decimal AverageDebt);
