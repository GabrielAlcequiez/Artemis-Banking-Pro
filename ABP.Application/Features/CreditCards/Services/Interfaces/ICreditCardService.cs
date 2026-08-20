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

    Task<CreditCardDetailDto?> GetClientDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientCreditCardPortfolioItemDto>>
        GetClientActiveCardsAsync(
            CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ClientCreditCardPortfolioItemDto>>(
            Array.Empty<ClientCreditCardPortfolioItemDto>());

    Task<CardOperationResult<Guid>> CreateAsync(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken = default);

    Task<CardOperationResult> UpdateLimitAsync(
        UpdateCreditLimitRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CancelAsync(
        CancelCreditCardRequest request,
        CancellationToken cancellationToken = default);
}
