namespace ABP.WebApi.Models.Commerce;

public sealed class CommerceListApiRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Status { get; set; }
}
