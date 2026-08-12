using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Home;

public sealed class ClientHomeViewModel
{
    public IReadOnlyCollection<ClientCreditCardPortfolioItemDto> CreditCards { get; init; } =
        Array.Empty<ClientCreditCardPortfolioItemDto>();
}
