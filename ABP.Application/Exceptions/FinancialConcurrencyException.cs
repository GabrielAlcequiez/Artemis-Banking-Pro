namespace ABP.Application.Exceptions;

public sealed class FinancialConcurrencyException : Exception
{
    public FinancialConcurrencyException(Exception innerException)
        : base(
            "La operación no pudo completarse porque los datos fueron modificados por otro proceso. Actualice la información e intente nuevamente.",
            innerException)
    {
    }
}
