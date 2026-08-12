using System.Globalization;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Exceptions;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CashAdvanceService(
    ICreditCardRepository creditCards,
    ISavingsAccountRepository accounts,
    IAccountBalanceService balances,
    IAccountLedger ledger,
    IUnitOfWork unitOfWork,
    IFinancialTransaction financialTransaction,
    ICurrentUserService currentUser,
    IClock clock,
    IValidator<CashAdvanceRequest> validator) : ICashAdvanceService
{
    public async Task<ClientCardOperationOptions> GetClientOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = GetCurrentClientId();
        if (clientId is null)
        {
            return EmptyOptions();
        }

        var cards = await creditCards.GetActiveByClientIdAsync(
            clientId,
            cancellationToken);
        var activeAccounts = await accounts.GetActiveByOwnerIdAsync(
            clientId,
            cancellationToken);

        return new ClientCardOperationOptions(
            cards.Select(card => new CreditCardOperationOptionDto(
                    card.Id,
                    $"************{LastFour(card.CardNumber)}",
                    card.Debt,
                    card.AvailableCredit,
                    card.ExpirationDate.ToString(
                        "MM/yy",
                        CultureInfo.InvariantCulture)))
                .ToArray(),
            activeAccounts.Select(account =>
                    new SavingsAccountOperationOptionDto(
                        account.Id,
                        account.AccountNumber,
                        account.Balance))
                .ToArray());
    }

    public async Task<OperationResult<FinancialOperationReceipt>> ProcessCashAdvanceAsync(
        CashAdvanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var clientId = GetCurrentClientId();
        if (clientId is null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.RoleNotAllowed);
        }

        var previousConsumption = await creditCards.GetConsumptionByOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (previousConsumption is not null)
        {
            return ResolveReplay(previousConsumption, request, clientId);
        }

        var card = await creditCards.GetByIdAsync(
            request.CreditCardId,
            cancellationToken);
        if (card is null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.CardNotFound);
        }

        var account = await accounts.GetByIdAsync(
            request.TargetAccountId,
            cancellationToken);
        if (account is null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.AccountNotFound);
        }

        try
        {
            return await financialTransaction.ExecuteAsync(
                async transactionCancellationToken =>
            {
                var trackedCard = await creditCards.GetForUpdateAsync(
                    request.CreditCardId,
                    transactionCancellationToken);
                var currentAccount = await accounts.GetByIdAsync(
                    request.TargetAccountId,
                    transactionCancellationToken);

                if (trackedCard is null || currentAccount is null)
                {
                    return OperationResult<FinancialOperationReceipt>.Failure(
                        trackedCard is null
                            ? CardFinancialOperationErrors.CardNotFound
                            : CardFinancialOperationErrors.AccountNotFound);
                }

                var currentValidationError = ValidateProducts(
                    trackedCard,
                    currentAccount,
                    clientId,
                    request.Amount,
                    clock.Today);
                var totalCharge = CreditCardRules.CalculateCashAdvanceTotal(
                    request.Amount);
                var processedAtUtc = clock.UtcNow;

                if (currentValidationError is not null)
                {
                    await creditCards.AddConsumptionAsync(
                        CreateConsumption(
                            request,
                            clientId,
                            totalCharge,
                            ConsumptionStatus.Rejected,
                            processedAtUtc,
                            currentValidationError),
                        transactionCancellationToken);
                    await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                    return OperationResult<FinancialOperationReceipt>.Failure(
                        currentValidationError);
                }

                var creditResult = await balances.CreditAsync(
                    currentAccount.Id,
                    request.Amount,
                    transactionCancellationToken);
                if (creditResult.IsFailure)
                {
                    var error = CardFinancialOperationErrors.AccountInactive;
                    await creditCards.AddConsumptionAsync(
                        CreateConsumption(
                            request,
                            clientId,
                            totalCharge,
                            ConsumptionStatus.Rejected,
                            processedAtUtc,
                            error),
                        transactionCancellationToken);
                    await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                    return OperationResult<FinancialOperationReceipt>.Failure(
                        error);
                }

                trackedCard.Debt += totalCharge;
                await creditCards.AddConsumptionAsync(
                    CreateConsumption(
                        request,
                        clientId,
                        totalCharge,
                        ConsumptionStatus.Approved,
                        processedAtUtc),
                    transactionCancellationToken);
                await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                await ledger.RecordApprovedAsync(
                    request.OperationId,
                    currentAccount.Id,
                    request.Amount,
                    TransactionDirection.Credit,
                    FinancialOperationType.CashAdvance,
                    LastFour(trackedCard.CardNumber),
                    currentAccount.AccountNumber,
                    clientId,
                    Roles.Client.ToString(),
                    transactionCancellationToken);

                return OperationResult<FinancialOperationReceipt>.Success(
                    new FinancialOperationReceipt(
                        request.OperationId,
                        request.Amount,
                        processedAtUtc));
            },
            cancellationToken);
        }
        catch (Exception exception)
            when (exception is PersistenceConflictException or
                  FinancialConcurrencyException)
        {
            var concurrentConsumption = await creditCards.GetConsumptionByOperationIdAsync(
                request.OperationId,
                cancellationToken);
            if (concurrentConsumption is null)
            {
                throw;
            }

            return ResolveReplay(concurrentConsumption, request, clientId);
        }
    }

    private string? GetCurrentClientId() =>
        currentUser.IsAuthenticated &&
        currentUser.IsInRole(Roles.Client.ToString()) &&
        !string.IsNullOrWhiteSpace(currentUser.UserId)
            ? currentUser.UserId
            : null;

    private static Error? ValidateProducts(
        Domain.Entities.CreditCards.CreditCard card,
        Domain.Entities.Accounts.SavingsAccount account,
        string clientId,
        decimal amount,
        DateOnly bankingDate)
    {
        if (card.ClientId != clientId || account.OwnerUserId != clientId)
        {
            return CardFinancialOperationErrors.OwnershipRequired;
        }

        if (card.Status != CreditCardStatus.Active)
        {
            return CardFinancialOperationErrors.CardInactive;
        }

        if (CreditCardRules.IsExpired(card.ExpirationDate, bankingDate))
        {
            return CardFinancialOperationErrors.CardExpired;
        }

        if (account.Status != SavingsAccountStatus.Active)
        {
            return CardFinancialOperationErrors.AccountInactive;
        }

        return CreditCardRules.CalculateCashAdvanceTotal(amount) >
               card.AvailableCredit
            ? CardFinancialOperationErrors.InsufficientCredit
            : null;
    }

    private static CardConsumption CreateConsumption(
        CashAdvanceRequest request,
        string actorUserId,
        decimal amount,
        ConsumptionStatus status,
        DateTimeOffset occurredAtUtc,
        Error? error = null) =>
        new()
        {
            CreditCardId = request.CreditCardId,
            CommerceId = null,
            TargetAccountId = request.TargetAccountId,
            CommerceName = "AVANCE",
            RequestedAmount = request.Amount,
            Amount = amount,
            Status = status,
            OccurredAtUtc = occurredAtUtc,
            OperationId = request.OperationId,
            ActorUserId = actorUserId,
            FailureCode = error?.Code,
            FailureDescription = error?.Description
        };

    private static OperationResult<FinancialOperationReceipt> ResolveReplay(
        CardConsumption consumption,
        CashAdvanceRequest request,
        string actorUserId)
    {
        if (consumption.ActorUserId != actorUserId ||
            consumption.CreditCardId != request.CreditCardId ||
            consumption.TargetAccountId != request.TargetAccountId ||
            consumption.RequestedAmount != request.Amount)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.OperationIdConflict);
        }

        return consumption.Status == ConsumptionStatus.Approved
            ? OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    consumption.OperationId,
                    consumption.RequestedAmount.Value,
                    consumption.OccurredAtUtc))
            : OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.ResolvePersisted(
                    consumption.FailureCode,
                    consumption.FailureDescription));
    }

    private static ClientCardOperationOptions EmptyOptions() =>
        new(
            Array.Empty<CreditCardOperationOptionDto>(),
            Array.Empty<SavingsAccountOperationOptionDto>());

    private static string LastFour(string cardNumber) =>
        cardNumber[^4..];
}
