using System.Transactions;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Notifications;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanPaymentService(
    ILoanRepository loanRepository,
    ISavingsAccountRepository savingsAccountRepository,
    IUserRepository userRepository,
    IAccountBalanceService accountBalanceService,
    IAccountLedger accountLedger,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IClock clock,
    IValidator<LoanPaymentRequest> requestValidator,
    IEmailService emailService,
    ILogger<LoanPaymentService> logger)
    : ILoanPaymentService
{
    public async Task<ClientLoanPaymentOptions> GetClientOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = GetCurrentClientId();

        if (clientId is null)
        {
            return EmptyClientOptions();
        }

        var loan = await loanRepository.GetActiveByClientIdAsync(
            clientId,
            cancellationToken);
        var accounts = await savingsAccountRepository.GetActiveByOwnerIdAsync(
            clientId,
            cancellationToken);
        var loans = loan is null
            ? Array.Empty<LoanOperationOptionDto>()
            :
            [
                new LoanOperationOptionDto(
                    loan.Id,
                    loan.LoanNumber,
                    loan.PendingAmount)
            ];

        return new ClientLoanPaymentOptions(
            loans,
            accounts
                .Select(account => new SavingsAccountOperationOptionDto(
                    account.Id,
                    account.AccountNumber,
                    account.Balance))
                .ToArray());
    }

    public async Task<OperationResult<CashierLoanPaymentPreview>> PrepareCashierPaymentAsync(
        string sourceAccountNumber,
        string loanNumber,
        decimal amount,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsCurrentCashier())
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.CashierRequired);
        }

        if (amount <= 0m || operationId == Guid.Empty)
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.InvalidPayment);
        }

        var normalizedAccountNumber = sourceAccountNumber?.Trim() ?? string.Empty;
        var normalizedLoanNumber = loanNumber?.Trim() ?? string.Empty;

        if (!IsNineDigitNumber(normalizedAccountNumber))
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.SourceAccountNotFound);
        }

        if (!IsNineDigitNumber(normalizedLoanNumber))
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.NotFound);
        }

        var account = await savingsAccountRepository.GetByAccountNumberAsync(
            normalizedAccountNumber,
            cancellationToken);

        if (account is null)
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.SourceAccountNotFound);
        }

        var loan = await loanRepository.GetByLoanNumberAsync(
            normalizedLoanNumber,
            cancellationToken);

        if (loan is null)
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.NotFound);
        }

        var validationError = ValidateProducts(loan, account, amount);

        if (validationError != Error.None)
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                validationError);
        }

        var accountOwner = await userRepository.GetByIdAsync(
            account.OwnerUserId,
            cancellationToken);
        var loanOwner = await userRepository.GetByIdAsync(
            loan.ClientId,
            cancellationToken);

        if (accountOwner is null || loanOwner is null)
        {
            return OperationResult<CashierLoanPaymentPreview>.Failure(
                LoanErrors.NotFound);
        }

        return OperationResult<CashierLoanPaymentPreview>.Success(
            new CashierLoanPaymentPreview(
                loan.Id,
                account.Id,
                operationId,
                FullName(accountOwner.Name, accountOwner.LastName),
                account.AccountNumber,
                FullName(loanOwner.Name, loanOwner.LastName),
                loan.LoanNumber,
                amount,
                Math.Min(amount, loan.PendingAmount)));
    }

    public async Task<LoanOperationResult<LoanPaymentResult>> ProcessPaymentAsync(
        LoanPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var actor = GetPaymentActor();

        if (actor is null)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    LoanErrors.PaymentActorRequired));
        }

        var previousPayment = await loanRepository
            .GetPaymentByOperationIdAsync(
                request.OperationId,
                cancellationToken);

        if (previousPayment is not null)
        {
            return WithoutNotification(
                BuildIdempotentResult(
                    request,
                    actor.Value.UserId,
                    previousPayment));
        }

        var loan = await loanRepository.GetWithInstallmentsAsync(
            request.LoanId,
            cancellationToken);

        if (loan is null)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    LoanErrors.NotFound));
        }

        if (loan.Status != LoanStatus.Active)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    LoanErrors.Inactive));
        }

        var sourceAccount = await savingsAccountRepository.GetByIdAsync(
            request.SourceAccountId,
            cancellationToken);

        if (sourceAccount is null)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    LoanErrors.SourceAccountNotFound));
        }

        var authorizationError = ValidateOwnership(
            actor.Value,
            loan,
            sourceAccount);

        if (authorizationError != Error.None)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    authorizationError));
        }

        var productError = ValidateProducts(
            loan,
            sourceAccount,
            request.Amount);

        if (productError != Error.None)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(productError));
        }

        var outstandingAmount = loan.Installments.Sum(
            installment => installment.PendingAmount);

        if (outstandingAmount <= 0m || loan.PendingAmount <= 0m)
        {
            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    LoanErrors.NoOutstandingBalance));
        }

        var effectiveAmount = Math.Min(
            request.Amount,
            outstandingAmount);
        var loanOwner = await userRepository.GetByIdAsync(
            loan.ClientId,
            cancellationToken);
        var accountOwner = loan.ClientId == sourceAccount.OwnerUserId
            ? loanOwner
            : await userRepository.GetByIdAsync(
                sourceAccount.OwnerUserId,
                cancellationToken);
        var processedAtUtc = clock.UtcNow;
        var processedAtBankingTime = clock.Now;
        OperationResult debitResult;

        using (var transaction = new TransactionScope(
                   TransactionScopeOption.Required,
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            debitResult = await accountBalanceService.DebitAsync(
                sourceAccount.Id,
                effectiveAmount,
                cancellationToken);

            if (debitResult.IsSuccess)
            {
                var settledAnyInstallment = ApplyPayment(
                    loan,
                    effectiveAmount);

                var payment = new LoanPayment
                {
                    LoanId = loan.Id,
                    SourceAccountId = sourceAccount.Id,
                    EffectiveAmount = effectiveAmount,
                    ActorUserId = actor.Value.UserId,
                    PaidAtUtc = processedAtUtc,
                    OperationId = request.OperationId
                };

                await loanRepository.AddPaymentAsync(
                    payment,
                    cancellationToken);
                await accountLedger.RecordApprovedAsync(
                    request.OperationId,
                    sourceAccount.Id,
                    effectiveAmount,
                    TransactionDirection.Debit,
                    FinancialOperationType.LoanPayment,
                    sourceAccount.AccountNumber,
                    loan.LoanNumber,
                    actor.Value.UserId,
                    actor.Value.Role,
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                if (settledAnyInstallment)
                {
                    await loanRepository.ClearLateFlagFromPaidInstallmentsAsync(
                        loan.Id,
                        processedAtUtc,
                        actor.Value.UserId,
                        cancellationToken);
                }

                transaction.Complete();
            }
        }

        if (debitResult.IsFailure)
        {
            await accountLedger.RecordRejectedAsync(
                sourceAccount.Id,
                request.OperationId,
                effectiveAmount,
                TransactionDirection.Debit,
                FinancialOperationType.LoanPayment,
                debitResult.Error.Description,
                actor.Value.UserId,
                actor.Value.Role,
                cancellationToken);

            logger.LogWarning(
                "Pago {OperationId} rechazado para el préstamo {LoanId}: {ErrorCode}.",
                request.OperationId,
                loan.Id,
                debitResult.Error.Code);

            return WithoutNotification(
                OperationResult<LoanPaymentResult>.Failure(
                    debitResult.Error));
        }

        logger.LogInformation(
            "Pago {OperationId} aplicado al préstamo {LoanId}. Solicitado: {RequestedAmount}; efectivo: {EffectiveAmount}; pendiente: {PendingAmount}.",
            request.OperationId,
            loan.Id,
            request.Amount,
            effectiveAmount,
            loan.PendingAmount);

        var notificationSent = await SendPaymentNotificationsAsync(
            request.OperationId,
            loan,
            sourceAccount,
            loanOwner,
            accountOwner,
            effectiveAmount,
            processedAtBankingTime);

        return new LoanOperationResult<LoanPaymentResult>(
            OperationResult<LoanPaymentResult>.Success(
                CreateResult(
                    request,
                    effectiveAmount,
                    loan.PendingAmount,
                    loan.Status == LoanStatus.Completed,
                    loan.LoanNumber,
                    processedAtUtc)),
            !notificationSent);
    }

    private OperationResult<LoanPaymentResult> BuildIdempotentResult(
        LoanPaymentRequest request,
        string actorUserId,
        LoanPayment previousPayment)
    {
        if (previousPayment.LoanId != request.LoanId ||
            previousPayment.SourceAccountId != request.SourceAccountId ||
            previousPayment.ActorUserId != actorUserId)
        {
            return OperationResult<LoanPaymentResult>.Failure(
                LoanErrors.OperationConflict);
        }

        var loan = previousPayment.Loan;

        logger.LogInformation(
            "Pago idempotente {OperationId} recuperado sin aplicar un nuevo débito.",
            request.OperationId);

        return OperationResult<LoanPaymentResult>.Success(
            CreateResult(
                request,
                previousPayment.EffectiveAmount,
                loan.PendingAmount,
                loan.Status == LoanStatus.Completed,
                loan.LoanNumber,
                previousPayment.PaidAtUtc));
    }

    private (string UserId, string Role)? GetPaymentActor()
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

    private string? GetCurrentClientId() =>
        currentUser.IsAuthenticated
        && currentUser.IsInRole(nameof(Roles.Client))
        && !string.IsNullOrWhiteSpace(currentUser.UserId)
            ? currentUser.UserId
            : null;

    private bool IsCurrentCashier() =>
        currentUser.IsAuthenticated
        && currentUser.IsInRole(nameof(Roles.Cashier))
        && !string.IsNullOrWhiteSpace(currentUser.UserId);

    private static ClientLoanPaymentOptions EmptyClientOptions() =>
        new(
            Array.Empty<LoanOperationOptionDto>(),
            Array.Empty<SavingsAccountOperationOptionDto>());

    private static bool IsNineDigitNumber(string value) =>
        value.Length == 9 && value.All(char.IsDigit);

    private static string FullName(string name, string lastName) =>
        $"{name} {lastName}".Trim();

    private static Error ValidateProducts(
        Loan loan,
        SavingsAccount sourceAccount,
        decimal requestedAmount)
    {
        if (loan.Status != LoanStatus.Active)
        {
            return LoanErrors.Inactive;
        }

        if (sourceAccount.Status != SavingsAccountStatus.Active)
        {
            return LoanErrors.SourceAccountInactive;
        }

        if (loan.PendingAmount <= 0m)
        {
            return LoanErrors.NoOutstandingBalance;
        }

        var effectiveAmount = Math.Min(requestedAmount, loan.PendingAmount);

        return sourceAccount.Balance < effectiveAmount
            ? LoanErrors.InsufficientFunds
            : Error.None;
    }

    private static Error ValidateOwnership(
        (string UserId, string Role) actor,
        Loan loan,
        SavingsAccount sourceAccount)
    {
        if (actor.Role != Roles.Client.ToString())
        {
            return Error.None;
        }

        if (loan.ClientId != actor.UserId)
        {
            return LoanErrors.LoanOwnershipRequired;
        }

        return sourceAccount.OwnerUserId != actor.UserId
            ? LoanErrors.AccountOwnershipRequired
            : Error.None;
    }

    private static bool ApplyPayment(Loan loan, decimal amount)
    {
        var remainingPayment = amount;
        var settledAnyInstallment = false;

        foreach (var installment in loan.Installments
                     .Where(item => item.PendingAmount > 0m)
                     .OrderBy(item => item.Number))
        {
            if (remainingPayment <= 0m)
            {
                break;
            }

            var appliedAmount = Math.Min(
                remainingPayment,
                installment.PendingAmount);
            installment.PendingAmount -= appliedAmount;
            remainingPayment -= appliedAmount;

            if (installment.PendingAmount == 0m)
            {
                installment.PaymentStatus = InstallmentPaymentStatus.Paid;
                installment.IsLate = false;
                settledAnyInstallment = true;
            }
            else
            {
                installment.PaymentStatus =
                    InstallmentPaymentStatus.PartiallyPaid;
            }
        }

        loan.PendingAmount = loan.Installments.Sum(
            installment => installment.PendingAmount);

        if (loan.PendingAmount == 0m)
        {
            loan.Status = LoanStatus.Completed;
        }

        return settledAnyInstallment;
    }

    private async Task<bool> SendPaymentNotificationsAsync(
        Guid operationId,
        Loan loan,
        SavingsAccount sourceAccount,
        ABP.Domain.Entities.User? loanOwner,
        ABP.Domain.Entities.User? accountOwner,
        decimal effectiveAmount,
        DateTimeOffset processedAtBankingTime)
    {
        var sourceAccountLastFourDigits = LastFour(
            sourceAccount.AccountNumber);
        var loanOwnerNotified =
            await LoanNotificationEmails.SendBestEffortAsync(
                emailService,
                logger,
                LoanNotificationEmails.LoanPayment(
                    ToRecipient(loanOwner, loan.ClientId),
                    loan.LoanNumber,
                    sourceAccountLastFourDigits,
                    effectiveAmount,
                    processedAtBankingTime),
                "pago al propietario del préstamo",
                operationId.ToString("N"));

        if (loan.ClientId == sourceAccount.OwnerUserId)
        {
            return loanOwnerNotified;
        }

        var accountOwnerNotified =
            await LoanNotificationEmails.SendBestEffortAsync(
                emailService,
                logger,
                LoanNotificationEmails.PaymentAccountDebit(
                    ToRecipient(accountOwner, sourceAccount.OwnerUserId),
                    loan.LoanNumber,
                    sourceAccountLastFourDigits,
                    effectiveAmount,
                    processedAtBankingTime),
                "débito al propietario de la cuenta",
                operationId.ToString("N"));

        return loanOwnerNotified && accountOwnerNotified;
    }

    private static LoanNotificationRecipient ToRecipient(
        ABP.Domain.Entities.User? user,
        string userId) =>
        new(
            userId,
            user?.Email ?? string.Empty,
            user is null
                ? string.Empty
                : FullName(user.Name, user.LastName));

    private static string LastFour(string value) =>
        value.Length <= 4
            ? value
            : value[^4..];

    private static LoanOperationResult<TValue> WithoutNotification<TValue>(
        OperationResult<TValue> result) =>
        new(result, false);

    private static LoanPaymentResult CreateResult(
        LoanPaymentRequest request,
        decimal effectiveAmount,
        decimal remainingAmount,
        bool isCompleted,
        string loanNumber,
        DateTimeOffset processedAt) =>
        new(
            request.LoanId,
            loanNumber,
            request.SourceAccountId,
            request.Amount,
            effectiveAmount,
            remainingAmount,
            isCompleted,
            request.OperationId,
            processedAt);
}
