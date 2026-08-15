using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Dashboards.DTOs;

public sealed record ClientPortfolioDto(
    IReadOnlyCollection<ClientSavingsAccountPortfolioItemDto> Accounts,
    ClientLoanPortfolioItemDto? ActiveLoan,
    IReadOnlyCollection<ClientCreditCardPortfolioItemDto> CreditCards)
{
    public bool HasProducts =>
        Accounts.Count > 0 || ActiveLoan is not null || CreditCards.Count > 0;
}
