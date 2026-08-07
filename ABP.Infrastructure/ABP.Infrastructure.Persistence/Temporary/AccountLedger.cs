using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Infrastructure.Persistence.Temporary
{
    // TEMPORAL - Implementación provisional hasta que P2 termine la suya
    // Al entregar P2: eliminar esta carpeta y cambiar los registros DI en ServicesRegistration.cs.
    public sealed class AccountLedger : IAccountLedger
    {
        private readonly IGenericRepository<AccountTransaction, Guid> _repository;

        public AccountLedger(IGenericRepository<AccountTransaction, Guid> repository)
        {
            _repository = repository;
        }

        public async Task RecordApprovedAsync(Guid operationId, Guid accountId, decimal amount,
            TransactionDirection direction, FinancialOperationType operationType, string? origin,
            string? beneficiary, string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            var transaction = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = operationId,
                Amount = amount,
                Direction = direction,
                OperationType = operationType,
                Origin = origin,
                Beneficiary = beneficiary,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Status = TransactionStatus.Approved
            };

            await _repository.AddAsync(transaction, cancellationToken);
        }

        public async Task RecordRejectedAsync(Guid accountId, Guid operationId, decimal amount,
            TransactionDirection direction, FinancialOperationType operationType, string rejectionReason,
            string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            var transaction = new AccountTransaction(Guid.NewGuid())
            {
                AccountId = accountId,
                OperationId = operationId,
                Amount = amount,
                Direction = direction,
                OperationType = operationType,
                RejectionReason = rejectionReason,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Status = TransactionStatus.Rejected
            };

            await _repository.AddAsync(transaction, cancellationToken);
        }
    }
}
