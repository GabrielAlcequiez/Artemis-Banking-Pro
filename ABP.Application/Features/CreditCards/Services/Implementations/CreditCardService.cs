using ABP.Application.Common;
using System.Globalization;
using System.Data;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Notifications;
using ABP.Application.Exceptions;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Domain.Rules.Cards;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CreditCardService(
    ICvcService cvcService,
    ICardNumberGeneratorService numberGeneratorService,
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IFinancialTransaction financialTransaction,
    IMapper mapper,
    IClock clock,
    ICurrentUserService currentUser,
    IValidator<CreditCardListRequest> listValidator,
    IValidator<CreateCreditCardRequest> createValidator,
    IValidator<UpdateCreditLimitRequest> updateLimitValidator,
    IValidator<CancelCreditCardRequest> cancelValidator,
    IUserRepository users,
    IEmailService emailService,
    ILogger<CreditCardService> logger) : ICreditCardService
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

    public async Task<CreditCardDetailDto?> GetClientDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default)
    {
        var clientId =
            currentUser.IsAuthenticated &&
            currentUser.IsInRole(Roles.Client.ToString())
                ? currentUser.UserId
                : null;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var readModel = await repository.GetDetailsForClientAsync(
            creditCardId,
            clientId,
            cancellationToken);

        return readModel is null
            ? null
            : mapper.Map<CreditCardDetailDto>(readModel);
    }

    public async Task<IReadOnlyCollection<ClientCreditCardPortfolioItemDto>>
        GetClientActiveCardsAsync(
            CancellationToken cancellationToken = default)
    {
        var clientId =
            currentUser.IsAuthenticated &&
            currentUser.IsInRole(Roles.Client.ToString())
                ? currentUser.UserId
                : null;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Array.Empty<ClientCreditCardPortfolioItemDto>();
        }

        var cards = await repository.GetActiveByClientIdAsync(
            clientId,
            cancellationToken);

        return cards
            .Select(card => new ClientCreditCardPortfolioItemDto(
                card.Id,
                $"************{card.CardNumber[^4..]}",
                card.Limit,
                card.Debt,
                card.ExpirationDate.ToString(
                    "MM/yy",
                    CultureInfo.InvariantCulture)))
            .ToArray();
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

    public async Task<CardOperationResult<Guid>> CreateAsync(
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
            return new CardOperationResult<Guid>(
                OperationResult<Guid>.Failure(
                    CreditCardErrors.AdministratorRequired),
                false);
        }

        var existingCard = await repository.GetByCreationOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (existingCard is not null)
        {
            return WithoutNotification(
                ResolveCreationReplay(existingCard, request, assignedByUserId));
        }

        AssignmentNotification? notification = null;
        OperationResult<Guid> result;

        try
        {
            result = await financialTransaction.ExecuteAsync(
                IsolationLevel.Serializable,
                async transactionCancellationToken =>
                {
                    existingCard = await repository.GetByCreationOperationIdAsync(
                        request.OperationId,
                        transactionCancellationToken);
                    if (existingCard is not null)
                    {
                        return ResolveCreationReplay(
                            existingCard,
                            request,
                            assignedByUserId);
                    }

                var clientExists = await repository.ClientExistsAsync(
                    request.ClientId,
                    transactionCancellationToken);

                if (!clientExists)
                {
                    return OperationResult<Guid>.Failure(
                        CreditCardErrors.ClientNotFound);
                }

                var isActiveClient = await repository.IsActiveClientAsync(
                    request.ClientId,
                    transactionCancellationToken);

                if (!isActiveClient)
                {
                    return OperationResult<Guid>.Failure(
                        CreditCardErrors.ClientInactive);
                }

                var cardNumber = await GenerateUniqueCardNumberAsync(
                    transactionCancellationToken);

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
                    AssignedByUserId = assignedByUserId,
                    CreationOperationId = request.OperationId
                };

                var createdCard = await repository.AddAsync(
                    card,
                    transactionCancellationToken);

                var client = await users.GetByIdAsync(
                    request.ClientId,
                    transactionCancellationToken);
                notification = new AssignmentNotification(
                    createdCard.Id,
                    ToRecipient(client, request.ClientId),
                    LastFour(cardNumber),
                    request.CreditLimit,
                    expirationDate,
                    clock.Now);


                await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                return OperationResult<Guid>.Success(createdCard.Id);
                },
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is PersistenceConflictException or
                  FinancialConcurrencyException)
        {
            existingCard = await repository.GetByCreationOperationIdAsync(
                request.OperationId,
                cancellationToken);
            if (existingCard is null)
            {
                throw;
            }

            result = ResolveCreationReplay(existingCard, request, assignedByUserId);
            notification = null;
        }

        if (result.IsFailure)
        {
            return new CardOperationResult<Guid>(result, false);
        }

        var notificationSent = notification is not null &&
            await CardNotificationEmails.SendBestEffortAsync(
                emailService,
                logger,
                CardNotificationEmails.Assignment(
                    notification.Recipient,
                    notification.CardLastFourDigits,
                    notification.CreditLimit,
                    notification.ExpirationDate,
                    notification.AssignedAtBankingTime),
                "asignación de tarjeta",
                notification.CreditCardId.ToString("N"));

        return new CardOperationResult<Guid>(result, !notificationSent);
    }

    private static CardOperationResult<Guid> WithoutNotification(
        OperationResult<Guid> result) =>
        new(result, false);

    private static OperationResult<Guid> ResolveCreationReplay(
        CreditCard card,
        CreateCreditCardRequest request,
        string assignedByUserId) =>
        card.AssignedByUserId == assignedByUserId &&
        card.ClientId == request.ClientId &&
        card.Limit == request.CreditLimit
            ? OperationResult<Guid>.Success(card.Id)
            : OperationResult<Guid>.Failure(
                CreditCardErrors.CreationOperationConflict);

    public async Task<CardOperationResult> UpdateLimitAsync(
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
            return new CardOperationResult(
                OperationResult.Failure(CreditCardErrors.NotFound),
                false);
        }

        if (card.Status == CreditCardStatus.Cancelled)
        {
            return new CardOperationResult(
                OperationResult.Failure(CreditCardErrors.Cancelled),
                false);
        }

        if (!CreditCardRules.CanChangeLimit(card.Status, card.Debt, request.CreditLimit))
        {
            return new CardOperationResult(
                OperationResult.Failure(CreditCardErrors.LimitBelowDebt),
                false);
        }

        card.Limit = request.CreditLimit;

        var client = await users.GetByIdAsync(
            card.ClientId,
            cancellationToken);
        var recipient = ToRecipient(client, card.ClientId);
        var cardLastFourDigits = LastFour(card.CardNumber);
        var changedAtBankingTime = clock.Now;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var notificationSent = await CardNotificationEmails.SendBestEffortAsync(
            emailService,
            logger,
            CardNotificationEmails.LimitChanged(
                recipient,
                cardLastFourDigits,
                request.CreditLimit,
                changedAtBankingTime),
            "modificación de límite",
            card.Id.ToString("N"));

        return new CardOperationResult(
            OperationResult.Success(),
            !notificationSent);
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

    private static CardNotificationRecipient ToRecipient(
        Domain.Entities.User? user,
        string userId) =>
        new(
            userId,
            user?.Email ?? string.Empty,
            user is null
                ? string.Empty
                : $"{user.Name} {user.LastName}".Trim());

    private static string LastFour(string value) => value[^4..];

    private sealed record AssignmentNotification(
        Guid CreditCardId,
        CardNotificationRecipient Recipient,
        string CardLastFourDigits,
        decimal CreditLimit,
        DateOnly ExpirationDate,
        DateTimeOffset AssignedAtBankingTime);
    #endregion
}
