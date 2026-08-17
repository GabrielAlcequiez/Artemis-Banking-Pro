using ABP.Domain.Common;
using ABP.Domain.Enums;
namespace ABP.Domain.Entities.CreditCards
{
    public sealed class CreditCard : AuditableEntity<Guid>
    {
        public string ClientId { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CvcHash { get; set; } = string.Empty;
        public decimal Limit { get; set; }
        public decimal Debt { get; set; }
        // Expira el último día calendario del mes (MM/AA) y es válida durante todo ese día (UTC/bancario).
        public DateOnly ExpirationDate { get; set; }
        public CreditCardStatus Status { get; set; } = CreditCardStatus.Active;
        public string AssignedByUserId { get; set; } = string.Empty;
        public Guid CreationOperationId { get; set; } = Guid.NewGuid();
        public byte[] RowVersion { get; set; } = [];

        public decimal AvailableCredit => Limit - Debt;
    }

}
