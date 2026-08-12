namespace ABP.Application.Exceptions;

public sealed class FinancialConcurrencyException : Exception
{
    public FinancialConcurrencyException()
        : base(
            "La operación no pudo completarse porque los datos fueron modificados por otro proceso. Actualice la información e intente nuevamente.")
    {
    }

    public FinancialConcurrencyException(Exception innerException)
        : this()
    {
        ArgumentNullException.ThrowIfNull(innerException);
    }
}
