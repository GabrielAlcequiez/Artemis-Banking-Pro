using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities.Accounts;

public class SavingsAccount : AuditableEntity<Guid>
{
    public SavingsAccount()
    {
    }

    public SavingsAccount(Guid id)
        : base(id)
    {
    }

    public string OwnerUserId { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public SavingsAccountType Type { get; set; }

    public SavingsAccountStatus Status { get; set; }

    public byte[] RowVersion { get; set; } = [];
}