using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Domain.Rules.Cards;
using AutoMapper;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CreditCardService(
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IValidator<CreditCardListRequest> listValidator,
    IValidator<UpdateCreditLimitRequest> updateLimitValidator,
    IValidator<CancelCreditCardRequest> cancelValidator) : ICreditCardService
{
    public async Task<CreditCardListResult> ListAsync(
        CreditCardListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await listValidator.ValidateAndThrowAsync(request, cancellationToken);

        var identification = NormalizeIdentification(request.Identification);
        var normalizedRequest = request with { Identification = identification };

        if (identification is not null)
        {
            var clientId = await repository.FindClientIdByIdentificationAsync(
                identification,
                cancellationToken);

            if (clientId is null)
            {
                return new(CreateEmptyPage(request), CreditCardSearchStatus.ClientNotFound);
            }

            if (!await repository.HasAnyCardsAsync(clientId, cancellationToken))
            {
                return new(CreateEmptyPage(request), CreditCardSearchStatus.ClientWithoutCards);
            }
        }

        var readPage = await repository.SearchAsync(
            normalizedRequest.Page,
            normalizedRequest.PageSize,
            normalizedRequest.Identification,
            normalizedRequest.Status,
            cancellationToken);

        var searchStatus = identification is null && !normalizedRequest.Status.HasValue
            ? CreditCardSearchStatus.NoSearch
            : readPage.TotalRecords == 0
                ? CreditCardSearchStatus.NoMatchingCards
                : CreditCardSearchStatus.ResultsFound;

        return new(MapPage(readPage), searchStatus);
    }

    public async Task<CreditCardDetailDto?> GetDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default)
    {
        var readModel = await repository.GetDetailsAsync(creditCardId, cancellationToken);

        return readModel is null
            ? null
            : mapper.Map<CreditCardDetailDto>(readModel);
    }

    private PagedResult<CreditCardSummaryDto> MapPage(
        PagedResult<CreditCardSummaryReadModel> page)
    {
        var data = mapper.Map<IReadOnlyCollection<CreditCardSummaryDto>>(page.Data);

        return new(data, page.Page, page.PageSize, page.TotalRecords);
    }

    private static PagedResult<CreditCardSummaryDto> CreateEmptyPage(
        CreditCardListRequest request) =>
        new(Array.Empty<CreditCardSummaryDto>(), request.Page, request.PageSize, 0);

    private static string? NormalizeIdentification(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();

    public Task<OperationResult<Guid>> CreateAsync(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Credit card creation is pending the shared auditing infrastructure.");
    }

    public async Task<OperationResult> UpdateLimitAsync(
        UpdateCreditLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await updateLimitValidator.ValidateAndThrowAsync(request, cancellationToken);

        var card = await repository.GetForUpdateAsync(
            request.CreditCardId,
            cancellationToken);

        if (card is null)
        {
            return OperationResult.Failure(CreditCardErrors.NotFound);
        }

        if (card.Status == CreditCardStatus.Cancelled)
        {
            return OperationResult.Failure(CreditCardErrors.Cancelled);
        }

        if (!CreditCardRules.CanChangeLimit(card.Status, card.Debt, request.CreditLimit))
        {
            return OperationResult.Failure(CreditCardErrors.LimitBelowDebt);
        }

        card.Limit = request.CreditLimit;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> CancelAsync(
        CancelCreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await cancelValidator.ValidateAndThrowAsync(request, cancellationToken);

        var card = await repository.GetForUpdateAsync(
            request.CreditCardId,
            cancellationToken);

        if (card is null)
        {
            return OperationResult.Failure(CreditCardErrors.NotFound);
        }

        if (card.Status == CreditCardStatus.Cancelled)
        {
            return OperationResult.Failure(CreditCardErrors.Cancelled);
        }

        if (!CreditCardRules.CanCancel(card.Status, card.Debt))
        {
            return OperationResult.Failure(CreditCardErrors.OutstandingDebt);
        }

        card.Status = CreditCardStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
