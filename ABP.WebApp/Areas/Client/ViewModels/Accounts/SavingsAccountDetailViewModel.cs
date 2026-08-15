using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Accounts;

public sealed class SavingsAccountDetailViewModel
{
    public required SavingsAccountDetailDto Account { get; init; }
}
