using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities.Lending
{
    public sealed class LoanInstallment : AuditableEntity<Guid>
    {
        public Guid LoanId { get; set; }
        public int Number { get; set; }
        public DateOnly DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public InstallmentPaymentStatus PaymentStatus { get; set; } = InstallmentPaymentStatus.Pending;
        public bool IsLate { get; set; }
    }
}
