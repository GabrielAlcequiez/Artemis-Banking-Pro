using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class CreditCardDetailViewModel
{
    public required CreditCardDetailDto Card { get; init; }
}
