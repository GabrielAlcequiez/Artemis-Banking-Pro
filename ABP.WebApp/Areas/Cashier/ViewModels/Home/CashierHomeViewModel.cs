using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Cashier.ViewModels.Home;

public sealed class CashierHomeViewModel
{
    public required CashierDailyOperationsSummaryDto Summary { get; init; }
}
