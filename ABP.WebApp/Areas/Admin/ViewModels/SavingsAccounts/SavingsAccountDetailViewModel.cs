using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;

public sealed class SavingsAccountDetailViewModel
{
    public required SavingsAccountDetailDto Account { get; init; }
}
