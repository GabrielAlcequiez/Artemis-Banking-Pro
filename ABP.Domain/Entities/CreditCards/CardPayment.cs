using ABP.Domain.Common;

namespace ABP.Domain.Entities.CreditCards
{
    public sealed class CardPayment : AuditableEntity<Guid>
    {
        public Guid CreditCardId { get; set; }
        public Guid SourceAccountId { get; set; }
        public decimal EffectiveAmount { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public DateTimeOffset PaidAtUtc { get; set; }
        public Guid OperationId { get; set; }
    }
}