using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces;

public interface ICreditCardService
{
    Task<CreditCardListResult> ListAsync(
        CreditCardListRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditCardDetailDto?> GetDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreateAsync(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateLimitAsync(
        UpdateCreditLimitRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CancelAsync(
        CancelCreditCardRequest request,
        CancellationToken cancellationToken = default);
}
