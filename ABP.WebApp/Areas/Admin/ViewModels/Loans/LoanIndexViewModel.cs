using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Common;

namespace ABP.WebApp.Areas.Admin.ViewModels.Loans;

public sealed class LoanIndexViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    public string? Status { get; set; }

    public PagedResult<LoanSummaryDto>? Result { get; set; }
}
