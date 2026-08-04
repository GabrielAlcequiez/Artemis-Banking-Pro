using ABP.Domain.Common;
using ABP.Domain.Entities;

namespace ABP.Domain.Entities.Lending
{
    public sealed class LoanPayment : AuditableEntity<Guid>
    {
        public Guid LoanId { get; set; }
        public Loan Loan { get; set; } = null!;
        public Guid SourceAccountId { get; set; }
        public SavingsAccount SourceAccount { get; set; } = null!;
        public decimal EffectiveAmount { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public User ActorUser { get; set; } = null!;
        public DateTimeOffset PaidAtUtc { get; set; }
        public Guid OperationId { get; set; }
    }
}
