namespace ABP.Application.Exceptions;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException()
        : base("La operación entra en conflicto con datos que ya existen.")
    {
    }

    public PersistenceConflictException(Exception innerException)
        : this()
    {
        ArgumentNullException.ThrowIfNull(innerException);
    }
}
