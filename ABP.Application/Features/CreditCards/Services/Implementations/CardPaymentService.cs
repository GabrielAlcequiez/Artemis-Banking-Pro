using System.Globalization;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CardPaymentService(
    ICreditCardRepository creditCards,
    ISavingsAccountRepository accounts,
    IUserRepository users,
    IAccountTransactionRepository accountTransactions,
    IAccountBalanceService balances,
    IAccountLedger ledger,
    IUnitOfWork unitOfWork,
    IFinancialTransaction financialTransaction,
    ICurrentUserService currentUser,
    IClock clock,
    IValidator<CreditCardPaymentRequest> validator) : ICardPaymentService
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
            cards.Select(ToCardOption).ToArray(),
            activeAccounts
                .Select(account => new SavingsAccountOperationOptionDto(
                    account.Id,
                    account.AccountNumber,
                    account.Balance))
                .ToArray());
    }

    public async Task<OperationResult<CashierCardPaymentPreview>> PrepareCashierPaymentAsync(
        string sourceAccountNumber,
        string creditCardNumber,
        decimal amount,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsCurrentCashier())
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.RoleNotAllowed);
        }

        if (amount <= 0m || operationId == Guid.Empty)
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                new Error(
                    "CreditCards.InvalidPayment",
                    "El monto a pagar debe ser mayor que cero."));
        }

        var normalizedAccountNumber = sourceAccountNumber?.Trim() ?? string.Empty;
        var normalizedCardNumber = creditCardNumber?.Trim() ?? string.Empty;

        if (normalizedCardNumber.Length != 16 ||
            normalizedCardNumber.Any(character => !char.IsDigit(character)))
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.CardNotFound);
        }

        var account = await accounts.GetByAccountNumberAsync(
            normalizedAccountNumber,
            cancellationToken);
        if (account is null)
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.AccountNotFound);
        }

        var card = await creditCards.GetByCardNumberAsync(
            normalizedCardNumber,
            cancellationToken);
        if (card is null)
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.CardNotFound);
        }

        var validationError = ValidateProducts(card, account, amount);
        if (validationError is not null)
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(validationError);
        }

        var accountOwner = await users.GetByIdAsync(
            account.OwnerUserId,
            cancellationToken);
        var cardOwner = await users.GetByIdAsync(
            card.ClientId,
            cancellationToken);
        if (accountOwner is null || cardOwner is null)
        {
            return OperationResult<CashierCardPaymentPreview>.Failure(
                CardFinancialOperationErrors.CardNotFound);
        }

        var effectiveAmount = Math.Min(amount, card.Debt);

        return OperationResult<CashierCardPaymentPreview>.Success(
            new CashierCardPaymentPreview(
                card.Id,
                account.Id,
                operationId,
                FullName(accountOwner.Name, accountOwner.LastName),
                account.AccountNumber,
                FullName(cardOwner.Name, cardOwner.LastName),
                LastFour(card.CardNumber),
                amount,
                effectiveAmount));
    }

    public async Task<OperationResult<FinancialOperationReceipt>> ProcessPaymentAsync(
        CreditCardPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var actor = GetActor();
        if (actor is null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.RoleNotAllowed);
        }

        var previousPayment = await creditCards.GetPaymentByOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (previousPayment is not null &&
            previousPayment.ActorUserId == actor.Value.UserId)
        {
            return OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    previousPayment.OperationId,
                    previousPayment.EffectiveAmount,
                    previousPayment.PaidAtUtc));
        }

        var previousLedgerEntries = await accountTransactions.GetByOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (previousLedgerEntries.Any(entry =>
                entry.ActorUserId == actor.Value.UserId &&
                entry.Status == TransactionStatus.Rejected))
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.InsufficientFunds);
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
            request.SourceAccountId,
            cancellationToken);
        if (account is null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                CardFinancialOperationErrors.AccountNotFound);
        }

        var ownershipError = ValidateOwnership(
            actor.Value.Role,
            actor.Value.UserId,
            card.ClientId,
            account.OwnerUserId);
        if (ownershipError is not null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(ownershipError);
        }

        var validationError = ValidateProducts(card, account, request.Amount);
        if (validationError is not null)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(validationError);
        }

        return await financialTransaction.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var trackedCard = await creditCards.GetForUpdateAsync(
                    request.CreditCardId,
                    transactionCancellationToken);
                var currentAccount = await accounts.GetByIdAsync(
                    request.SourceAccountId,
                    transactionCancellationToken);

                if (trackedCard is null || currentAccount is null)
                {
                    return OperationResult<FinancialOperationReceipt>.Failure(
                        trackedCard is null
                            ? CardFinancialOperationErrors.CardNotFound
                            : CardFinancialOperationErrors.AccountNotFound);
                }

                var currentOwnershipError = ValidateOwnership(
                    actor.Value.Role,
                    actor.Value.UserId,
                    trackedCard.ClientId,
                    currentAccount.OwnerUserId);
                if (currentOwnershipError is not null)
                {
                    return OperationResult<FinancialOperationReceipt>.Failure(
                        currentOwnershipError);
                }

                var currentValidationError = ValidateProducts(
                    trackedCard,
                    currentAccount,
                    request.Amount);
                if (currentValidationError is not null)
                {
                    return OperationResult<FinancialOperationReceipt>.Failure(
                        currentValidationError);
                }

                var effectiveAmount = Math.Min(request.Amount, trackedCard.Debt);
                var debitResult = await balances.DebitAsync(
                    currentAccount.Id,
                    effectiveAmount,
                    transactionCancellationToken);

                if (debitResult.IsFailure)
                {
                    await ledger.RecordRejectedAsync(
                        currentAccount.Id,
                        request.OperationId,
                        effectiveAmount,
                        TransactionDirection.Debit,
                        FinancialOperationType.CreditCardPayment,
                        CardFinancialOperationErrors.InsufficientFunds.Description,
                        actor.Value.UserId,
                        actor.Value.Role,
                        transactionCancellationToken);

                    return OperationResult<FinancialOperationReceipt>.Failure(
                        CardFinancialOperationErrors.InsufficientFunds);
                }

                trackedCard.Debt -= effectiveAmount;
                var processedAtUtc = clock.UtcNow;
                await creditCards.AddPaymentAsync(
                    new CardPayment
                    {
                        CreditCardId = trackedCard.Id,
                        SourceAccountId = currentAccount.Id,
                        EffectiveAmount = effectiveAmount,
                        ActorUserId = actor.Value.UserId,
                        PaidAtUtc = processedAtUtc,
                        OperationId = request.OperationId
                    },
                    transactionCancellationToken);
                await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                await ledger.RecordApprovedAsync(
                    request.OperationId,
                    currentAccount.Id,
                    effectiveAmount,
                    TransactionDirection.Debit,
                    FinancialOperationType.CreditCardPayment,
                    currentAccount.AccountNumber,
                    LastFour(trackedCard.CardNumber),
                    actor.Value.UserId,
                    actor.Value.Role,
                    transactionCancellationToken);

                return OperationResult<FinancialOperationReceipt>.Success(
                    new FinancialOperationReceipt(
                        request.OperationId,
                        effectiveAmount,
                        processedAtUtc));
            },
            cancellationToken);
    }

    private string? GetCurrentClientId() =>
        currentUser.IsAuthenticated &&
        currentUser.IsInRole(Roles.Client.ToString()) &&
        !string.IsNullOrWhiteSpace(currentUser.UserId)
            ? currentUser.UserId
            : null;

    private bool IsCurrentCashier() =>
        currentUser.IsAuthenticated &&
        currentUser.IsInRole(Roles.Cashier.ToString()) &&
        !string.IsNullOrWhiteSpace(currentUser.UserId);

    private (string UserId, string Role)? GetActor()
    {
        if (!currentUser.IsAuthenticated ||
            string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return null;
        }

        if (currentUser.IsInRole(Roles.Client.ToString()))
        {
            return (currentUser.UserId, Roles.Client.ToString());
        }

        return currentUser.IsInRole(Roles.Cashier.ToString())
            ? (currentUser.UserId, Roles.Cashier.ToString())
            : null;
    }

    private static Error? ValidateOwnership(
        string actorRole,
        string actorUserId,
        string cardOwnerId,
        string accountOwnerId) =>
        actorRole == Roles.Client.ToString() &&
        (cardOwnerId != actorUserId || accountOwnerId != actorUserId)
            ? CardFinancialOperationErrors.OwnershipRequired
            : null;

    private static Error? ValidateProducts(
        Domain.Entities.CreditCards.CreditCard card,
        Domain.Entities.Accounts.SavingsAccount account,
        decimal requestedAmount)
    {
        if (card.Status != CreditCardStatus.Active)
        {
            return CardFinancialOperationErrors.CardInactive;
        }

        if (account.Status != SavingsAccountStatus.Active)
        {
            return CardFinancialOperationErrors.AccountInactive;
        }

        if (card.Debt <= 0m)
        {
            return CardFinancialOperationErrors.CardWithoutDebt;
        }

        var effectiveAmount = Math.Min(requestedAmount, card.Debt);
        return account.Balance < effectiveAmount
            ? CardFinancialOperationErrors.InsufficientFunds
            : null;
    }

    private static ClientCardOperationOptions EmptyOptions() =>
        new(
            Array.Empty<CreditCardOperationOptionDto>(),
            Array.Empty<SavingsAccountOperationOptionDto>());

    private static CreditCardOperationOptionDto ToCardOption(
        Domain.Entities.CreditCards.CreditCard card) =>
        new(
            card.Id,
            $"************{LastFour(card.CardNumber)}",
            card.Debt,
            card.AvailableCredit,
            card.ExpirationDate.ToString("MM/yy", CultureInfo.InvariantCulture));

    private static string LastFour(string cardNumber) =>
        cardNumber[^4..];

    private static string FullName(string name, string lastName) =>
        $"{name} {lastName}".Trim();
}
