namespace ABP.Domain.Common;

public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(TKey id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; protected set; }

    public string? CreatedByUserId { get; protected set; }

    public DateTimeOffset? LastModifiedAtUtc { get; protected set; }

    public string? LastModifiedByUserId { get; protected set; }
}
