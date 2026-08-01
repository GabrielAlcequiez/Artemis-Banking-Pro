using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities;

public class AccountTransaction : AuditableEntity<Guid>
{
    public AccountTransaction()
    {
    }

    public AccountTransaction(Guid id)
        : base(id)
    {
    }

    public Guid AccountId { get; set; }

    public Guid OperationId { get; set; }

    public decimal Amount { get; set; }

    public TransactionDirection Direction { get; set; }

    public FinancialOperationType OperationType { get; set; }

    public string? Origin { get; set; }

    public string? Beneficiary { get; set; }

    public TransactionStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public string? ActorUserId { get; set; }

    public string? ActorRole { get; set; }
}
