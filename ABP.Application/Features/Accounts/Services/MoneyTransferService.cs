using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Notifications;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    public sealed class MoneyTransferService : IMoneyTransferService
    {
        private readonly ISavingsAccountRepository _accounts;
        private readonly IAccountBalanceService _balances;
        private readonly IAccountLedger _ledger;
        private readonly IFinancialTransaction _financialTransaction;
        private readonly IGenericRepository<User, string> _users;
        private readonly IEmailService _emailService;
        private readonly IClock _clock;
        private readonly ILogger<MoneyTransferService> _logger;

        public MoneyTransferService(
            ISavingsAccountRepository accounts,
            IAccountBalanceService balances,
            IAccountLedger ledger,
            IFinancialTransaction financialTransaction,
            IGenericRepository<User, string> users,
            IEmailService emailService,
            IClock clock,
            ILogger<MoneyTransferService> logger)
        {
            _accounts = accounts;
            _balances = balances;
            _ledger = ledger;
            _financialTransaction = financialTransaction;
            _users = users;
            _emailService = emailService;
            _clock = clock;
            _logger = logger;
        }

        public async Task<OperationResult<FinancialOperationReceipt>> TransferAsync(
            TransferFundsRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.InvalidAmount);
            }

            var source = await _accounts.GetByIdAsync(request.SourceAccountId, cancellationToken);
            if (source is null)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.NotFound);
            }

            var destination = request.DestinationAccountId is not null
                ? await _accounts.GetByIdAsync(request.DestinationAccountId.Value, cancellationToken)
                : await _accounts.GetByAccountNumberAsync(request.DestinationAccountNumber ?? string.Empty, cancellationToken);

            if (destination is null)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.NotFound);
            }

            if (destination.Id == source.Id)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.SameAccount);
            }

            if (request.OperationType == FinancialOperationType.OwnAccountTransfer)
            {
                var ownerActiveAccounts = await _accounts.GetActiveByOwnerIdAsync(source.OwnerUserId, cancellationToken);
                if (ownerActiveAccounts.Count < 2)
                {
                    return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.NotEnoughActiveAccounts);
                }
            }

            var operationId = Guid.NewGuid();

            var result = await _financialTransaction.ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var debitResult = await _balances.DebitAsync(source.Id, request.Amount, transactionCancellationToken);
                    if (debitResult.IsFailure)
                    {
                        await _ledger.RecordRejectedAsync(
                            source.Id, operationId, request.Amount, TransactionDirection.Debit,
                            request.OperationType, debitResult.Error.Description, request.ActorUserId, request.ActorRole,
                            transactionCancellationToken);

                        _logger.LogWarning(
                            "Transferencia {OperationId} rechazada al debitar la cuenta {AccountId}: {Reason}",
                            operationId, source.Id, debitResult.Error.Description);

                        return OperationResult<FinancialOperationReceipt>.Failure(debitResult.Error);
                    }

                    var creditResult = await _balances.CreditAsync(destination.Id, request.Amount, transactionCancellationToken);
                    if (creditResult.IsFailure)
                    {
                        await _ledger.RecordRejectedAsync(
                            destination.Id, operationId, request.Amount, TransactionDirection.Credit,
                            request.OperationType, creditResult.Error.Description, request.ActorUserId, request.ActorRole,
                            transactionCancellationToken);

                        _logger.LogError(
                            "Transferencia {OperationId} falló al acreditar la cuenta {AccountId} tras debitar {SourceId}.",
                            operationId, destination.Id, source.Id);

                        return OperationResult<FinancialOperationReceipt>.Failure(creditResult.Error);
                    }

                    await _ledger.RecordApprovedAsync(
                        operationId, source.Id, request.Amount, TransactionDirection.Debit,
                        request.OperationType, source.AccountNumber, destination.AccountNumber,
                        request.ActorUserId, request.ActorRole, transactionCancellationToken);

                    await _ledger.RecordApprovedAsync(
                        operationId, destination.Id, request.Amount, TransactionDirection.Credit,
                        request.OperationType, source.AccountNumber, destination.AccountNumber,
                        request.ActorUserId, request.ActorRole, transactionCancellationToken);

                    _logger.LogInformation(
                        "Transferencia {OperationId} de {Amount} completada: {SourceAccount} -> {DestinationAccount}.",
                        operationId, request.Amount, source.AccountNumber, destination.AccountNumber);

                    return OperationResult<FinancialOperationReceipt>.Success(
                        new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow));
                },
                cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            var processedAt = _clock.Now;

            if (request.OperationType == FinancialOperationType.OwnAccountTransfer)
            {
                await SendOwnAccountTransferEmailAsync(
                    source.OwnerUserId, source.AccountNumber, destination.AccountNumber, request.Amount,
                    processedAt, operationId, cancellationToken);
            }
            else
            {
                await SendTwoPartyTransferEmailsAsync(
                    source.OwnerUserId, source.AccountNumber, destination.OwnerUserId, destination.AccountNumber,
                    request.Amount, processedAt, operationId, cancellationToken);
            }

            return result;
        }

        public async Task<OperationResult<FinancialOperationReceipt>> DepositAsync(
            DepositRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.InvalidAmount);
            }

            var destination = await _accounts.GetByAccountNumberAsync(request.DestinationAccountNumber, cancellationToken);
            if (destination is null)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.NotFound);
            }

            var operationId = Guid.NewGuid();

            var result = await _financialTransaction.ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var creditResult = await _balances.CreditAsync(destination.Id, request.Amount, transactionCancellationToken);
                    if (creditResult.IsFailure)
                    {
                        await _ledger.RecordRejectedAsync(
                            destination.Id, operationId, request.Amount, TransactionDirection.Credit,
                            FinancialOperationType.Deposit, creditResult.Error.Description,
                            request.ActorUserId, request.ActorRole, transactionCancellationToken);

                        return OperationResult<FinancialOperationReceipt>.Failure(creditResult.Error);
                    }

                    await _ledger.RecordApprovedAsync(
                        operationId, destination.Id, request.Amount, TransactionDirection.Credit,
                        FinancialOperationType.Deposit, "DEPÓSITO", destination.AccountNumber,
                        request.ActorUserId, request.ActorRole, transactionCancellationToken);

                    _logger.LogInformation(
                        "Depósito {OperationId} de {Amount} aplicado a la cuenta {DestinationAccount} por {ActorUserId}.",
                        operationId, request.Amount, destination.AccountNumber, request.ActorUserId);

                    return OperationResult<FinancialOperationReceipt>.Success(
                        new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow));
                },
                cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            var recipient = await ResolveRecipientAsync(destination.OwnerUserId, cancellationToken);
            if (recipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.Deposit(recipient, LastFour(destination.AccountNumber), request.Amount, _clock.Now),
                    "Deposit", operationId.ToString());
            }

            return result;
        }

        public async Task<OperationResult<FinancialOperationReceipt>> WithdrawAsync(
            WithdrawalRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.InvalidAmount);
            }

            var source = await _accounts.GetByAccountNumberAsync(request.SourceAccountNumber, cancellationToken);
            if (source is null)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(AccountErrors.NotFound);
            }

            var operationId = Guid.NewGuid();

            var result = await _financialTransaction.ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var debitResult = await _balances.DebitAsync(source.Id, request.Amount, transactionCancellationToken);
                    if (debitResult.IsFailure)
                    {
                        await _ledger.RecordRejectedAsync(
                            source.Id, operationId, request.Amount, TransactionDirection.Debit,
                            FinancialOperationType.Withdrawal, debitResult.Error.Description,
                            request.ActorUserId, request.ActorRole, transactionCancellationToken);

                        _logger.LogWarning(
                            "Retiro {OperationId} rechazado en la cuenta {SourceAccount}: {Reason}",
                            operationId, source.AccountNumber, debitResult.Error.Description);

                        return OperationResult<FinancialOperationReceipt>.Failure(debitResult.Error);
                    }

                    await _ledger.RecordApprovedAsync(
                        operationId, source.Id, request.Amount, TransactionDirection.Debit,
                        FinancialOperationType.Withdrawal, source.AccountNumber, "RETIRO",
                        request.ActorUserId, request.ActorRole, transactionCancellationToken);

                    _logger.LogInformation(
                        "Retiro {OperationId} de {Amount} aplicado a la cuenta {SourceAccount} por {ActorUserId}.",
                        operationId, request.Amount, source.AccountNumber, request.ActorUserId);

                    return OperationResult<FinancialOperationReceipt>.Success(
                        new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow));
                },
                cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            var recipient = await ResolveRecipientAsync(source.OwnerUserId, cancellationToken);
            if (recipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.Withdrawal(recipient, LastFour(source.AccountNumber), request.Amount, _clock.Now),
                    "Withdrawal", operationId.ToString());
            }

            return result;
        }

        private async Task SendTwoPartyTransferEmailsAsync(
            string sourceOwnerUserId, string sourceAccountNumber,
            string destinationOwnerUserId, string destinationAccountNumber,
            decimal amount, DateTimeOffset processedAt, Guid operationId, CancellationToken cancellationToken)
        {
            var sourceRecipient = await ResolveRecipientAsync(sourceOwnerUserId, cancellationToken);
            if (sourceRecipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.TransferSent(
                        sourceRecipient, LastFour(destinationAccountNumber), amount, processedAt),
                    "TransferSent", operationId.ToString());
            }

            var destinationRecipient = await ResolveRecipientAsync(destinationOwnerUserId, cancellationToken);
            if (destinationRecipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.TransferReceived(
                        destinationRecipient, LastFour(sourceAccountNumber), amount, processedAt),
                    "TransferReceived", operationId.ToString());
            }
        }

        private async Task SendOwnAccountTransferEmailAsync(
            string ownerUserId, string sourceAccountNumber, string destinationAccountNumber,
            decimal amount, DateTimeOffset processedAt, Guid operationId, CancellationToken cancellationToken)
        {
            var recipient = await ResolveRecipientAsync(ownerUserId, cancellationToken);
            if (recipient is null)
            {
                return;
            }

            await AccountNotificationEmails.SendBestEffortAsync(
                _emailService, _logger,
                AccountNotificationEmails.OwnAccountTransfer(
                    recipient, LastFour(sourceAccountNumber), LastFour(destinationAccountNumber), amount, processedAt),
                "OwnAccountTransfer", operationId.ToString());
        }

        private async Task<AccountNotificationRecipient?> ResolveRecipientAsync(
            string ownerUserId, CancellationToken cancellationToken)
        {
            var owner = await _users.GetByIdAsync(ownerUserId, cancellationToken);
            if (owner is null || string.IsNullOrWhiteSpace(owner.Email))
            {
                return null;
            }

            return new AccountNotificationRecipient(owner.Id, owner.Email, $"{owner.Name} {owner.LastName}".Trim());
        }

        private static string LastFour(string accountNumber) =>
            accountNumber.Length <= 4 ? accountNumber : accountNumber[^4..];
    }
}
