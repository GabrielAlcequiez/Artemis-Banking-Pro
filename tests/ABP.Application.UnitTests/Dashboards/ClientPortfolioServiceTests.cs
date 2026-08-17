using ABP.Application.Features.Dashboards.Services.Implementations;
using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Dashboards;

public sealed class ClientPortfolioServiceTests
{
    [Fact]
    public async Task GetPortfolioAsync_orders_accounts_principal_first_then_descending_balance()
    {
        var clientId = "client-1";
        var accounts = new DashboardSavingsAccountRepository
        {
            OwnedAccounts =
            [
                Account(clientId, SavingsAccountType.Secondary, 100m, "200000002"),
                Account(clientId, SavingsAccountType.Principal, 1m, "200000001"),
                Account(clientId, SavingsAccountType.Secondary, 200m, "200000003")
            ]
        };
        var service = new ClientPortfolioService(
            accounts,
            new DashboardLoanService(),
            new DashboardCreditCardService(),
            new DashboardCurrentUser
            {
                IsAuthenticated = true,
                UserId = clientId,
                Roles = [Roles.Client.ToString()]
            });

        var result = await service.GetPortfolioAsync();

        Assert.Equal(3, result.Accounts.Count);
        Assert.Equal(SavingsAccountType.Principal, result.Accounts.ElementAt(0).Type);
        Assert.Equal("200000003", result.Accounts.ElementAt(1).AccountNumber);
        Assert.Equal("200000002", result.Accounts.ElementAt(2).AccountNumber);
    }

    [Fact]
    public async Task GetPortfolioAsync_no_active_products_returns_has_products_false()
    {
        var service = new ClientPortfolioService(
            new DashboardSavingsAccountRepository(),
            new DashboardLoanService(),
            new DashboardCreditCardService(),
            new DashboardCurrentUser
            {
                IsAuthenticated = true,
                UserId = "client-1",
                Roles = [Roles.Client.ToString()]
            });

        var result = await service.GetPortfolioAsync();

        Assert.False(result.HasProducts);
        Assert.Empty(result.Accounts);
        Assert.Null(result.ActiveLoan);
        Assert.Empty(result.CreditCards);
    }

    [Theory]
    [InlineData(false, "Client")]
    [InlineData(true, "Administrator")]
    public async Task GetPortfolioAsync_unauthenticated_or_non_client_returns_empty_portfolio(
        bool isAuthenticated,
        string role)
    {
        var accounts = new DashboardSavingsAccountRepository
        {
            OwnedAccounts = [Account("client-1", SavingsAccountType.Principal, 100m, "200000001")]
        };
        var service = new ClientPortfolioService(
            accounts,
            new DashboardLoanService
            {
                ActiveLoan = new ClientLoanPortfolioItemDto(
                    Guid.NewGuid(),
                    "900000001",
                    1000m,
                    900m,
                    100m,
                    10m,
                    12,
                    12,
                    1,
                    false)
            },
            new DashboardCreditCardService(),
            new DashboardCurrentUser
            {
                IsAuthenticated = isAuthenticated,
                UserId = "client-1",
                Roles = [role]
            });

        var result = await service.GetPortfolioAsync();

        Assert.False(result.HasProducts);
        Assert.Empty(result.Accounts);
        Assert.Null(result.ActiveLoan);
        Assert.Empty(result.CreditCards);
    }

    private static SavingsAccount Account(
        string ownerUserId,
        SavingsAccountType type,
        decimal balance,
        string accountNumber) =>
        new(Guid.NewGuid())
        {
            OwnerUserId = ownerUserId,
            Type = type,
            Balance = balance,
            AccountNumber = accountNumber,
            Status = SavingsAccountStatus.Active
        };
}
