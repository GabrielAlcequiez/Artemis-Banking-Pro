namespace ABP.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; protected set; }

    public string? CreatedByUserId { get; protected set; }

    public DateTimeOffset? LastModifiedAtUtc { get; protected set; }

    public string? LastModifiedByUserId { get; protected set; }
}
