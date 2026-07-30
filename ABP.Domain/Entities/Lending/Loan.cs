using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities.Lending
{
    public sealed class Loan : AuditableEntity<Guid>
    {
        public string ClientId { get; set; } = string.Empty;
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Capital { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Active;
        public string AssignedByUserId { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = [];
    }
}
