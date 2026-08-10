using ABP.Domain.Common;

namespace ABP.Domain.Entities.Accounts;

public class Beneficiary : AuditableEntity<Guid>
{
    public Beneficiary()
    {
    }

    public Beneficiary(Guid id)
        : base(id)
    {
    }

    public string OwnerUserId { get; set; } = string.Empty;

    public Guid BeneficiaryAccountId { get; set; }
}
