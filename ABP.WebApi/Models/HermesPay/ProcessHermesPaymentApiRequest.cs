namespace ABP.WebApi.Models.HermesPay;

public sealed class ProcessHermesPaymentApiRequest
{
    public string CardNumber { get; set; } = string.Empty;

    public string MonthExpirationCard { get; set; } = string.Empty;

    public string YearExpirationCard { get; set; } = string.Empty;

    public string Cvc { get; set; } = string.Empty;

    public decimal TransactionAmount { get; set; }
}
