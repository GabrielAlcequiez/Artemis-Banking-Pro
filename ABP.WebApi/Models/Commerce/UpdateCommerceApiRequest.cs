namespace ABP.WebApi.Models.Commerce;

public sealed class UpdateCommerceApiRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Rnc { get; set; } = string.Empty;
}
