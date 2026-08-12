using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.CreditCards;

public sealed class CreditCardDetailViewModel
{
    public required CreditCardDetailDto Card { get; init; }
}
