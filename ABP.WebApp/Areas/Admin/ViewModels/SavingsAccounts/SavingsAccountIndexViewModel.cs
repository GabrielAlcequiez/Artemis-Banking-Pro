using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;

namespace ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;

public sealed class SavingsAccountIndexViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? OwnerIdentification { get; set; }

    public string? Status { get; set; }

    public string? Type { get; set; }

    public PagedResult<SavingsAccountSummaryDto>? Result { get; set; }
}
