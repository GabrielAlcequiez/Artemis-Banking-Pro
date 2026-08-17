using System.Data;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.Notifications;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Exceptions;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardCommandHandler(
    ICvcService cvcService,
    ICardNumberGeneratorService numberGeneratorService,
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IFinancialTransaction financialTransaction,
    IClock clock,
    ICurrentUserService currentUser,
    IUserRepository users,
    IEmailService emailService,
    ILogger<CreateCreditCardCommandHandler> logger)
    : IRequestHandler<CreateCreditCardCommand, OperationResult<Guid>>
{
    private const int MaxCardNumberGenerationAttempts = 10;

    public async Task<OperationResult<Guid>> Handle(
        CreateCreditCardCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

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

        var existingCard = await repository.GetByCreationOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (existingCard is not null)
        {
            return ResolveReplay(existingCard, request, assignedByUserId);
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
                        return ResolveReplay(existingCard, request, assignedByUserId);
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
                var expirationDate = CreditCardRules.CalculateExpirationDate(clock.Today);
                var card = new CreditCard
                {
                    ClientId = request.ClientId,
                    CardNumber = cardNumber,
                    CvcHash = cvcService.Hash(cvc),
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

            result = ResolveReplay(existingCard, request, assignedByUserId);
            notification = null;
        }

        if (result.IsSuccess && notification is not null)
        {
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
        }

        return result;
    }

    private static OperationResult<Guid> ResolveReplay(
        CreditCard card,
        CreateCreditCardRequest request,
        string assignedByUserId) =>
        card.AssignedByUserId == assignedByUserId &&
        card.ClientId == request.ClientId &&
        card.Limit == request.CreditLimit
            ? OperationResult<Guid>.Success(card.Id)
            : OperationResult<Guid>.Failure(
                CreditCardErrors.CreationOperationConflict);

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

    private static CardNotificationRecipient ToRecipient(
        Domain.Entities.User? user,
        string userId) =>
        new(
            userId,
            user?.Email ?? string.Empty,
            user is null
                ? string.Empty
                : $"{user.Name} {user.LastName}".Trim());

    private static string LastFour(string cardNumber) => cardNumber[^4..];

    private sealed record AssignmentNotification(
        Guid CreditCardId,
        CardNotificationRecipient Recipient,
        string CardLastFourDigits,
        decimal CreditLimit,
        DateOnly ExpirationDate,
        DateTimeOffset AssignedAtBankingTime);
}
