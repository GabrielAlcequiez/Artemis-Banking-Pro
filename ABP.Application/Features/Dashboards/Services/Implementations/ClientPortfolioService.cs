using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.Dashboards.DTOs;
using ABP.Application.Features.Dashboards.Services.Interfaces;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Dashboards.Services.Implementations;

public sealed class ClientPortfolioService(
    ISavingsAccountRepository accounts,
    ILoanService loans,
    ICreditCardService creditCards,
    ICurrentUserService currentUser) : IClientPortfolioService
{
    public async Task<ClientPortfolioDto> GetPortfolioAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = currentUser.IsAuthenticated
            && currentUser.IsInRole(nameof(Roles.Client))
            && !string.IsNullOrWhiteSpace(currentUser.UserId)
                ? currentUser.UserId
                : null;

        if (clientId is null)
        {
            return new ClientPortfolioDto(
                Array.Empty<ClientSavingsAccountPortfolioItemDto>(),
                null,
                Array.Empty<ClientCreditCardPortfolioItemDto>());
        }

        var ownedAccounts = await accounts.GetActiveByOwnerIdAsync(
            clientId,
            cancellationToken);

        var accountItems = ownedAccounts
            .OrderBy(account => account.Type)
            .ThenByDescending(account => account.Balance)
            .Select(account => new ClientSavingsAccountPortfolioItemDto(
                account.Id,
                account.AccountNumber,
                account.Balance,
                account.Type))
            .ToArray();

        var activeLoan = await loans.GetClientActiveLoanAsync(cancellationToken);
        var activeCards = await creditCards.GetClientActiveCardsAsync(cancellationToken);

        return new ClientPortfolioDto(accountItems, activeLoan, activeCards);
    }
}
