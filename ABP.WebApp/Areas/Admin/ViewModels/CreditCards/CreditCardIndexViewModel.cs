using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class CreditCardIndexViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    public string? Status { get; set; }

    public CreditCardListResult? Result { get; set; }
}
