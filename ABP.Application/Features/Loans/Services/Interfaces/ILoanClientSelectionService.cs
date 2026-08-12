using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanClientSelectionService
{
    Task<LoanClientSelectionResult> SearchAsync(LoanClientSearchRequest request, CancellationToken cancellationToken = default);
    Task<LoanClientCandidateDto?> GetEligibleClientAsync(string clientId, CancellationToken cancellationToken = default);
}
