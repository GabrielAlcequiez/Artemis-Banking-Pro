using ABP.Application.Common.Services.Implementations;

namespace ABP.Application.UnitTests.Dashboards;

public sealed class CustomerDebtServiceTests
{
    [Fact]
    public async Task GetAverageActiveClientDebtAsync_zero_active_clients_returns_zero()
    {
        var users = new DashboardUserRepository();
        var loans = new DashboardLoanRepository
        {
            TotalActiveDebt = 500m
        };
        var creditCards = new DashboardCreditCardRepository
        {
            TotalActiveDebt = 250m
        };
        var service = new CustomerDebtService(users, loans, creditCards);

        var result = await service.GetAverageActiveClientDebtAsync();

        Assert.Equal(0.00m, result);
    }

    [Fact]
    public async Task GetAverageActiveClientDebtAsync_averages_loan_and_card_debt()
    {
        var users = new DashboardUserRepository
        {
            ActiveClientCount = 3
        };
        var loans = new DashboardLoanRepository
        {
            TotalActiveDebt = 300m
        };
        var creditCards = new DashboardCreditCardRepository
        {
            TotalActiveDebt = 150m
        };
        var service = new CustomerDebtService(users, loans, creditCards);

        var result = await service.GetAverageActiveClientDebtAsync();

        Assert.Equal(150m, result);
    }
}
