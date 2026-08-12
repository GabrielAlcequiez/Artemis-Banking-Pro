namespace ABP.Application.Features.Commerce.Commands;

internal sealed record NormalizedCommerceData(
    string Name,
    string? Description,
    string Email,
    string PhoneNumber,
    string Rnc);

internal static class CommerceDataNormalizer
{
    public static NormalizedCommerceData Normalize(
        string name,
        string? description,
        string email,
        string phoneNumber,
        string rnc) =>
        new(
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            email.Trim(),
            phoneNumber.Trim(),
            rnc.Trim());
}
