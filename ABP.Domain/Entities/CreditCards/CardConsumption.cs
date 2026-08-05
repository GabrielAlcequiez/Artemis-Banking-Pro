using ABP.Domain.Common;
using ABP.Domain.Enums;
namespace ABP.Domain.Entities.Cards
{
    public sealed class CardConsumption : AuditableEntity<Guid>
    {
        public Guid CreditCardId { get; set; }
        public Guid? CommerceId { get; set; }
        public string CommerceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public ConsumptionStatus Status { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
        public Guid OperationId { get; set; }
    }

}