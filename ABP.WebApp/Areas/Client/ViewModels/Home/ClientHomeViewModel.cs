using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Home;

public sealed class ClientHomeViewModel
{
    public IReadOnlyCollection<ClientCreditCardPortfolioItemDto> CreditCards { get; init; } =
        Array.Empty<ClientCreditCardPortfolioItemDto>();

    public IReadOnlyCollection<ABP.Application.Features.Accounts.DTOs.SavingsAccountOperationOptionDto> SavingsAccounts { get; init; } =
        Array.Empty<ABP.Application.Features.Accounts.DTOs.SavingsAccountOperationOptionDto>();
}
