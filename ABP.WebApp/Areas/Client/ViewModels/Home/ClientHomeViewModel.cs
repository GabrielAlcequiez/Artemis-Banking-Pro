using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.Dashboards.DTOs;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Home;

public sealed class ClientHomeViewModel
{
    public IReadOnlyCollection<ClientSavingsAccountPortfolioItemDto> Accounts { get; init; } =
        Array.Empty<ClientSavingsAccountPortfolioItemDto>();

    public ClientLoanPortfolioItemDto? ActiveLoan { get; init; }

    public IReadOnlyCollection<ClientCreditCardPortfolioItemDto> CreditCards { get; init; } =
        Array.Empty<ClientCreditCardPortfolioItemDto>();

    public bool HasProducts =>
        Accounts.Count > 0 || ActiveLoan is not null || CreditCards.Count > 0;
}
