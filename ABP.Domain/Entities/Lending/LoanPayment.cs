using ABP.Domain.Common;

namespace ABP.Domain.Entities.Lending
{
    public sealed class LoanPayment : AuditableEntity<Guid>
    {
        public Guid LoanId { get; set; }
        public Guid SourceAccountId { get; set; }
        public decimal EffectiveAmount { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public DateTimeOffset PaidAtUtc { get; set; }
        public Guid OperationId { get; set; }
    }
}
