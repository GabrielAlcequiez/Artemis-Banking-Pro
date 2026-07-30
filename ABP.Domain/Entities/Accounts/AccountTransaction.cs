using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ABP.Domain.Entities.Accounts
{
    public class AccountTransaction : AuditableEntity<Guid>
    {
        protected AccountTransaction()
        {
           
        }

        private AccountTransaction(
            Guid id,
            Guid accountId,
            Guid operationId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string? origin,
            string? beneficiary,
            TransactionStatus status,
            string? rejectionReason,
            string? actorUserId,
            string? actorRole)
            : base(id)
        {
            AccountId = accountId;
            OperationId = operationId;
            Amount = amount;
            Direction = direction;
            OperationType = operationType;
            Origin = origin;
            Beneficiary = beneficiary;
            Status = status;
            RejectionReason = rejectionReason;
            ActorUserId = actorUserId;
            ActorRole = actorRole;
        }


        public Guid AccountId { get; protected set; }
        public Guid OperationId { get; protected set; }

        public decimal Amount { get; protected set; }

        public TransactionDirection Direction { get; protected set; }

        public FinancialOperationType OperationType { get; protected set; }

        public string? Origin { get; protected set; }

        public string? Beneficiary { get; protected set; }

        public TransactionStatus Status { get; protected set; }

        public string? RejectionReason { get; protected set; }

        public string? ActorUserId { get; protected set; }

        public string? ActorRole { get; protected set; }


        public static AccountTransaction CreateApproved(
        Guid accountId,
        Guid operationId,
        decimal amount,
        TransactionDirection direction,
        FinancialOperationType operationType,
        string? origin,
        string? beneficiary,
        string? actorUserId,
        string? actorRole)
        {
            if (amount <= 0)
            {
                throw new InvalidMonetaryAmountException(amount);
            }

            return new AccountTransaction(
                Guid.NewGuid(),
                accountId,
                operationId,
                amount,
                direction,
                operationType,
                origin,
                beneficiary,
                TransactionStatus.Approved,
                rejectionReason: null,
                actorUserId,
                actorRole);
        }

        public static AccountTransaction CreateRejected(
            Guid accountId,
            Guid operationId,
            decimal amount,
            TransactionDirection direction,
            FinancialOperationType operationType,
            string rejectionReason,
            string? actorUserId,
            string? actorRole)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                throw new ArgumentException("A rejection reason is required.", nameof(rejectionReason));
            }

            return new AccountTransaction(
                Guid.NewGuid(),
                accountId,
                operationId,
                amount,
                direction,
                operationType,
                origin: null,
                beneficiary: null,
                TransactionStatus.Rejected,
                rejectionReason,
                actorUserId,
                actorRole);

        }

    }
}
