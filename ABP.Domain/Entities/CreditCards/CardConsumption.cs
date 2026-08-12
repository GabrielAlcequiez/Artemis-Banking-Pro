using ABP.Domain.Common;
using ABP.Domain.Enums;
namespace ABP.Domain.Entities.CreditCards
{
    public sealed class CardConsumption : AuditableEntity<Guid>
    {
        public Guid CreditCardId { get; set; }
        public Guid? CommerceId { get; set; }
        public Guid? TargetAccountId { get; set; }
        public string CommerceName { get; set; } = string.Empty;
        public decimal? RequestedAmount { get; set; }
        public decimal Amount { get; set; }
        public ConsumptionStatus Status { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
        public Guid OperationId { get; set; }
        public string? ActorUserId { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureDescription { get; set; }
    }

}
