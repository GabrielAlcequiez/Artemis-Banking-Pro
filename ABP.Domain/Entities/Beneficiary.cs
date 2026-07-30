using ABP.Domain.Common;

namespace ABP.Domain.Entities;

public class Beneficiary : AuditableEntity<Guid>
{
    protected Beneficiary()
    {
        
        OwnerUserId = string.Empty;
    }

    private Beneficiary(Guid id, string ownerUserId, Guid beneficiaryAccountId)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        BeneficiaryAccountId = beneficiaryAccountId;
    }

    public string OwnerUserId { get; protected set; }

    public Guid BeneficiaryAccountId { get; protected set; }





    public static Beneficiary Create(string ownerUserId, Guid beneficiaryAccountId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        return new Beneficiary(Guid.NewGuid(), ownerUserId, beneficiaryAccountId);
    }
}
