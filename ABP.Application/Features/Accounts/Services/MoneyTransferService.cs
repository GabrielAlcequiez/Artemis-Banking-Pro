using ABP.Application.Common;
using ABP.Application.Common.DTOs;
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
    /// <summary>
    /// Orchestrates two-leg money movement (debit + credit) and records both legs
    /// in the ledger under one OperationId.
    /// </summary>
    /// <remarks>
    /// Known gap: <see cref="IUnitOfWork"/> currently only wraps a single
    /// <c>SaveChangesAsync</c> with no Begin/Commit/Rollback, and
    /// <see cref="IAccountBalanceService"/>/<see cref="IAccountLedger"/> each commit
    /// their own step. So this is not yet atomic: if the credit leg fails after the
    /// debit succeeded, we issue a compensating credit back to the source instead of
    /// a real rollback. Once P1 adds transaction support to IUnitOfWork, this should
    /// be rewritten to wrap the whole operation in one transaction.
    /// </remarks>
    public sealed class MoneyTransferService : IMoneyTransferService
    {
        private readonly ISavingsAccountRepository _accounts;
        private readonly IAccountBalanceService _balances;
        private readonly IAccountLedger _ledger;
        private readonly IGenericRepository<User, string> _users;
        private readonly IEmailService _emailService;
        private readonly IClock _clock;
        private readonly ILogger<MoneyTransferService> _logger;

        public MoneyTransferService(
            ISavingsAccountRepository accounts,
            IAccountBalanceService balances,
            IAccountLedger ledger,
            IGenericRepository<User, string> users,
            IEmailService emailService,
            IClock clock,
            ILogger<MoneyTransferService> logger)
        {
            _accounts = accounts;
            _balances = balances;
            _ledger = ledger;
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

            var debitResult = await _balances.DebitAsync(source.Id, request.Amount, cancellationToken);
            if (debitResult.IsFailure)
            {
                await _ledger.RecordRejectedAsync(
                    source.Id, operationId, request.Amount, TransactionDirection.Debit,
                    request.OperationType, debitResult.Error.Description, request.ActorUserId, request.ActorRole,
                    cancellationToken);

                _logger.LogWarning(
                    "Transferencia {OperationId} rechazada al debitar la cuenta {AccountId}: {Reason}",
                    operationId, source.Id, debitResult.Error.Description);

                return OperationResult<FinancialOperationReceipt>.Failure(debitResult.Error);
            }

            var creditResult = await _balances.CreditAsync(destination.Id, request.Amount, cancellationToken);
            if (creditResult.IsFailure)
            {
                // Compensating action — see the "Known gap" note on this class.
                await _balances.CreditAsync(source.Id, request.Amount, cancellationToken);

                await _ledger.RecordRejectedAsync(
                    destination.Id, operationId, request.Amount, TransactionDirection.Credit,
                    request.OperationType, creditResult.Error.Description, request.ActorUserId, request.ActorRole,
                    cancellationToken);

                _logger.LogError(
                    "Transferencia {OperationId} falló al acreditar la cuenta {AccountId} tras debitar {SourceId}; se aplicó reverso.",
                    operationId, destination.Id, source.Id);

                return OperationResult<FinancialOperationReceipt>.Failure(creditResult.Error);
            }

            await _ledger.RecordApprovedAsync(
                operationId, source.Id, request.Amount, TransactionDirection.Debit,
                request.OperationType, source.AccountNumber, destination.AccountNumber,
                request.ActorUserId, request.ActorRole, cancellationToken);

            await _ledger.RecordApprovedAsync(
                operationId, destination.Id, request.Amount, TransactionDirection.Credit,
                request.OperationType, source.AccountNumber, destination.AccountNumber,
                request.ActorUserId, request.ActorRole, cancellationToken);

            _logger.LogInformation(
                "Transferencia {OperationId} de {Amount} completada: {SourceAccount} -> {DestinationAccount}.",
                operationId, request.Amount, source.AccountNumber, destination.AccountNumber);

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

            var receipt = new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow);
            return OperationResult<FinancialOperationReceipt>.Success(receipt);
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

            var creditResult = await _balances.CreditAsync(destination.Id, request.Amount, cancellationToken);
            if (creditResult.IsFailure)
            {
                await _ledger.RecordRejectedAsync(
                    destination.Id, operationId, request.Amount, TransactionDirection.Credit,
                    FinancialOperationType.Deposit, creditResult.Error.Description,
                    request.ActorUserId, request.ActorRole, cancellationToken);

                return OperationResult<FinancialOperationReceipt>.Failure(creditResult.Error);
            }

            await _ledger.RecordApprovedAsync(
                operationId, destination.Id, request.Amount, TransactionDirection.Credit,
                FinancialOperationType.Deposit, "DEPÓSITO", destination.AccountNumber,
                request.ActorUserId, request.ActorRole, cancellationToken);

            _logger.LogInformation(
                "Depósito {OperationId} de {Amount} aplicado a la cuenta {DestinationAccount} por {ActorUserId}.",
                operationId, request.Amount, destination.AccountNumber, request.ActorUserId);

            var recipient = await ResolveRecipientAsync(destination.OwnerUserId, cancellationToken);
            if (recipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.Deposit(recipient, LastFour(destination.AccountNumber), request.Amount, _clock.Now),
                    "Deposit", operationId.ToString());
            }

            var receipt = new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow);
            return OperationResult<FinancialOperationReceipt>.Success(receipt);
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

            var debitResult = await _balances.DebitAsync(source.Id, request.Amount, cancellationToken);
            if (debitResult.IsFailure)
            {
                await _ledger.RecordRejectedAsync(
                    source.Id, operationId, request.Amount, TransactionDirection.Debit,
                    FinancialOperationType.Withdrawal, debitResult.Error.Description,
                    request.ActorUserId, request.ActorRole, cancellationToken);

                _logger.LogWarning(
                    "Retiro {OperationId} rechazado en la cuenta {SourceAccount}: {Reason}",
                    operationId, source.AccountNumber, debitResult.Error.Description);

                return OperationResult<FinancialOperationReceipt>.Failure(debitResult.Error);
            }

            await _ledger.RecordApprovedAsync(
                operationId, source.Id, request.Amount, TransactionDirection.Debit,
                FinancialOperationType.Withdrawal, source.AccountNumber, "RETIRO",
                request.ActorUserId, request.ActorRole, cancellationToken);

            _logger.LogInformation(
                "Retiro {OperationId} de {Amount} aplicado a la cuenta {SourceAccount} por {ActorUserId}.",
                operationId, request.Amount, source.AccountNumber, request.ActorUserId);

            var recipient = await ResolveRecipientAsync(source.OwnerUserId, cancellationToken);
            if (recipient is not null)
            {
                await AccountNotificationEmails.SendBestEffortAsync(
                    _emailService, _logger,
                    AccountNotificationEmails.Withdrawal(recipient, LastFour(source.AccountNumber), request.Amount, _clock.Now),
                    "Withdrawal", operationId.ToString());
            }

            var receipt = new FinancialOperationReceipt(operationId, request.Amount, DateTimeOffset.UtcNow);
            return OperationResult<FinancialOperationReceipt>.Success(receipt);
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
