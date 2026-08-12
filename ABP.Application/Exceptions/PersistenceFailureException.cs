namespace ABP.Application.Exceptions;

public sealed class PersistenceFailureException : Exception
{
    public PersistenceFailureException()
        : base("No fue posible guardar los cambios solicitados.")
    {
    }

    public PersistenceFailureException(Exception innerException)
        : this()
    {
        ArgumentNullException.ThrowIfNull(innerException);
    }
}
