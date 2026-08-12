using ABP.Domain.Common;

namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanClientSelectionResult(
    PagedResult<LoanClientCandidateDto> Page,
    decimal AverageDebt);
