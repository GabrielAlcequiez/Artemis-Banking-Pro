using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Appends approved/rejected entries to a savings account's ledger.</summary>
    public sealed class AccountLedger : IAccountLedger
    {
        private readonly IAccountTransactionRepository _transactions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountLedger> _logger;

        public AccountLedger(
            IAccountTransactionRepository transactions,
            IUnitOfWork unitOfWork,
            ILogger<AccountLedger> logger)
        {
            _transactions = transactions;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecordApprovedAsync(
            Guid operationId, Guid accountId, decimal amount, TransactionDirection direction,
            FinancialOperationType operationType, string? origin, string? beneficiary,
            string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            var entry = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = operationId,
                Amount = amount,
                Direction = direction,
                OperationType = operationType,
                Origin = origin,
                Beneficiary = beneficiary,
                Status = TransactionStatus.Approved,
                ActorUserId = actorUserId,
                ActorRole = actorRole
            };

            await _transactions.AddAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Movimiento {OperationType} aprobado por {Amount} en la cuenta {AccountId} (operación {OperationId}).",
                operationType, amount, accountId, operationId);
        }

        public async Task RecordRejectedAsync(
            Guid accountId, Guid operationId, decimal amount, TransactionDirection direction,
            FinancialOperationType operationType, string rejectionReason,
            string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            var entry = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = operationId,
                Amount = amount,
                Direction = direction,
                OperationType = operationType,
                Status = TransactionStatus.Rejected,
                RejectionReason = rejectionReason,
                ActorUserId = actorUserId,
                ActorRole = actorRole
            };

            await _transactions.AddAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Movimiento {OperationType} rechazado en la cuenta {AccountId} (operación {OperationId}): {Reason}",
                operationType, accountId, operationId, rejectionReason);
        }
    }
}
