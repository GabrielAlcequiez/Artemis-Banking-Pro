using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities.CreditCards
{
    public sealed class CardPayment : AuditableEntity<Guid>
    {
        public Guid CreditCardId { get; set; }
        public Guid SourceAccountId { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal EffectiveAmount { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public DateTimeOffset PaidAtUtc { get; set; }
        public Guid OperationId { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Approved;
        public string? FailureCode { get; set; }
        public string? FailureDescription { get; set; }
    }
}
