using ABP.Application.Features.Dashboards.Services.Implementations;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Dashboards;

public sealed class AdminDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_calculates_all_metrics_correctly()
    {
        var users = new DashboardUserRepository
        {
            ActiveClientCount = 12,
            InactiveClientCount = 3
        };
        var accounts = new DashboardSavingsAccountRepository
        {
            ActiveAccountCount = 20
        };
        var loans = new DashboardLoanRepository
        {
            ActiveLoanCount = 4
        };
        var creditCards = new DashboardCreditCardRepository
        {
            ActiveCardCount = 5
        };
        var transactions = new DashboardTransactionRepository
        {
            TotalCount = 100,
            TodayCount = 7,
            TotalPaymentCount = 40,
            TodayPaymentCount = 2
        };
        var customerDebt = new FakeCustomerDebtService
        {
            AverageDebt = 123.45m
        };
        var clock = new DashboardClock(new DateOnly(2026, 8, 17));
        var service = new AdminDashboardService(
            users,
            accounts,
            loans,
            creditCards,
            transactions,
            customerDebt,
            clock);

        var result = await service.GetDashboardAsync();

        Assert.Equal(100, result.TotalTransactions);
        Assert.Equal(7, result.TodayTransactions);
        Assert.Equal(40, result.TotalPayments);
        Assert.Equal(2, result.TodayPayments);
        Assert.Equal(12, result.ActiveClients);
        Assert.Equal(3, result.InactiveClients);
        Assert.Equal(29, result.TotalFinancialProducts);
        Assert.Equal(4, result.ActiveLoans);
        Assert.Equal(5, result.ActiveCreditCards);
        Assert.Equal(20, result.ActiveSavingsAccounts);
        Assert.Equal(123.45m, result.AverageDebtPerClient);
    }

    [Fact]
    public async Task GetDashboardAsync_zero_active_clients_returns_zero_average_debt()
    {
        var users = new DashboardUserRepository();
        var customerDebt = new FakeCustomerDebtService
        {
            AverageDebt = 0m
        };
        var service = new AdminDashboardService(
            users,
            new DashboardSavingsAccountRepository(),
            new DashboardLoanRepository(),
            new DashboardCreditCardRepository(),
            new DashboardTransactionRepository(),
            customerDebt,
            new DashboardClock(new DateOnly(2026, 8, 17)));

        var result = await service.GetDashboardAsync();

        Assert.Equal(0.00m, result.AverageDebtPerClient);
    }

    [Fact]
    public async Task GetDashboardAsync_filters_today_transactions_and_payments()
    {
        var today = new DateOnly(2026, 8, 17);
        var transactions = new DashboardTransactionRepository
        {
            TodayCount = 8,
            TodayPaymentCount = 3
        };
        var service = new AdminDashboardService(
            new DashboardUserRepository(),
            new DashboardSavingsAccountRepository(),
            new DashboardLoanRepository(),
            new DashboardCreditCardRepository(),
            transactions,
            new FakeCustomerDebtService(),
            new DashboardClock(today));

        var result = await service.GetDashboardAsync();

        Assert.Equal(8, result.TodayTransactions);
        Assert.Equal(3, result.TodayPayments);
        Assert.Equal(2, transactions.RequestedDates.Count(date => date == today));
    }
}
