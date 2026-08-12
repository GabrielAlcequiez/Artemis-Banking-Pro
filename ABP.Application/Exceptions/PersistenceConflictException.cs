namespace ABP.Application.Exceptions;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(Exception innerException)
        : base(
            "La operación entra en conflicto con datos que ya existen.",
            innerException)
    {
    }
}
