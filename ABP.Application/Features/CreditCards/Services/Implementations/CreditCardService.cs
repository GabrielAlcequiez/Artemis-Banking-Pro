using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Domain.Rules.Cards;
using AutoMapper;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CreditCardService(
    ICvcService cvcService,
    ICardNumberGeneratorService numberGeneratorService,
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IClock clock,
    ICurrentUserService currentUser,
    IValidator<CreditCardListRequest> listValidator,
    IValidator<CreateCreditCardRequest> createValidator,
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

    public async Task<OperationResult<Guid>> CreateAsync(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await createValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var assignedByUserId =
            currentUser.IsAuthenticated &&
            currentUser.IsInRole(Roles.Administrator.ToString())
                ? currentUser.UserId
                : null;

        if (string.IsNullOrWhiteSpace(assignedByUserId))
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.AdministratorRequired);
        }

        var clientExists = await repository.ClientExistsAsync(
            request.ClientId,
            cancellationToken);

        if (!clientExists)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.ClientNotFound);
        }

        var isActiveClient = await repository.IsActiveClientAsync(
            request.ClientId,
            cancellationToken);

        if (!isActiveClient)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.ClientInactive);
        }

        var cardNumber = await GenerateUniqueCardNumberAsync(
            cancellationToken);

        if (cardNumber is null)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.NumberGenerationFailed);
        }

        var cvc = cvcService.Generate();
        var hashedCvc = cvcService.Hash(cvc);
        var expirationDate = CalculateExpirationDate();

        var card = new CreditCard
        {
            ClientId = request.ClientId,
            CardNumber = cardNumber,
            CvcHash = hashedCvc,
            Limit = request.CreditLimit,
            Debt = 0m,
            ExpirationDate = expirationDate,
            Status = CreditCardStatus.Active,
            AssignedByUserId = assignedByUserId
        };

        var createdCard = await repository.AddAsync(
            card,
            cancellationToken);

        // TODO(P1 Outbox): enqueue the assignment email in this transaction so it is
        // dispatched only after commit. Never include the full PAN, CVC, or CVC hash.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(createdCard.Id);
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

        // TODO(P1 Outbox): enqueue the limit-change email in this transaction so it is
        // dispatched only after commit. Include only the last four digits, never the PAN.
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

    #region Helpers
    private const int MaxCardNumberGenerationAttempts = 10;

    private async Task<string?> GenerateUniqueCardNumberAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0;
             attempt < MaxCardNumberGenerationAttempts;
             attempt++)
        {
            var candidate = numberGeneratorService.Generate();

            var alreadyExists = await repository.CardNumberExistsAsync(
                candidate,
                cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        return null;
    }

    private DateOnly CalculateExpirationDate()
    {
        var expirationMonth = clock.Today.AddYears(3);

        var lastDayOfMonth = DateTime.DaysInMonth(
            expirationMonth.Year,
            expirationMonth.Month);

        return new DateOnly(
            expirationMonth.Year,
            expirationMonth.Month,
            lastDayOfMonth);
    }
    #endregion
}
