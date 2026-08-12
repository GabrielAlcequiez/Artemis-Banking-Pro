using ABP.Domain.Enums;

namespace ABP.WebApi.Models.SavingsAccounts;

public sealed class SavingsAccountListApiRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? OwnerIdentification { get; set; }

    public SavingsAccountStatus? Status { get; set; }

    public SavingsAccountType? Type { get; set; }
}
