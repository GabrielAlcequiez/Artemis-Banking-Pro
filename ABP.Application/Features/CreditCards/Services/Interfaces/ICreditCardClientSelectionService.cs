using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces;

public interface ICreditCardClientSelectionService
{
    Task<CreditCardClientSelectionResult> SearchAsync(
        CreditCardClientSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditCardClientCandidateDto?> GetActiveClientAsync(
        string clientId,
        CancellationToken cancellationToken = default);
}
