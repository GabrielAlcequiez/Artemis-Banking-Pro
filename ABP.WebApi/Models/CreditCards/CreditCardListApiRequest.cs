namespace ABP.WebApi.Models.CreditCards;

public sealed class CreditCardListApiRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    public string? Status { get; set; }
}
