using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;

namespace ABP.TestDoubles
{
    /// <summary>Spy-style fake: records every call it receives instead of touching a store.</summary>
    public class FakeAccountLedger : IAccountLedger
    {
        public record ApprovedEntry(
            Guid OperationId, Guid AccountId, decimal Amount, TransactionDirection Direction,
            FinancialOperationType OperationType, string? Origin, string? Beneficiary,
            string? ActorUserId, string? ActorRole);

        public record RejectedEntry(
            Guid AccountId, Guid OperationId, decimal Amount, TransactionDirection Direction,
            FinancialOperationType OperationType, string RejectionReason,
            string? ActorUserId, string? ActorRole);

        public List<ApprovedEntry> RecordedApprovals { get; } = new();

        public List<RejectedEntry> RecordedRejections { get; } = new();

        public Task RecordApprovedAsync(
            Guid operationId, Guid accountId, decimal amount, TransactionDirection direction,
            FinancialOperationType operationType, string? origin, string? beneficiary,
            string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            RecordedApprovals.Add(new ApprovedEntry(
                operationId, accountId, amount, direction, operationType, origin, beneficiary, actorUserId, actorRole));
            return Task.CompletedTask;
        }

        public Task RecordRejectedAsync(
            Guid accountId, Guid operationId, decimal amount, TransactionDirection direction,
            FinancialOperationType operationType, string rejectionReason,
            string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            RecordedRejections.Add(new RejectedEntry(
                accountId, operationId, amount, direction, operationType, rejectionReason, actorUserId, actorRole));
            return Task.CompletedTask;
        }
    }
}
