namespace ABP.Domain.Common;

public abstract class BaseEntity<TKey>
{
    protected BaseEntity()
    {
    }

    protected BaseEntity(TKey id)
    {
        Id = id;
    }

    public TKey Id { get; protected set; }
}
