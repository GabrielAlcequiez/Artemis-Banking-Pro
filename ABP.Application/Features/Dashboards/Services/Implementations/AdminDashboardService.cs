using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.Dashboards.DTOs;
using ABP.Application.Features.Dashboards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Dashboards.Services.Implementations;

public sealed class AdminDashboardService(
    IUserRepository users,
    ISavingsAccountRepository accounts,
    ILoanRepository loans,
    ICreditCardRepository creditCards,
    IAccountTransactionRepository transactions,
    ICustomerDebtService customerDebt,
    IClock clock) : IAdminDashboardService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;

        var totalTransactions = await transactions.CountAllAsync(cancellationToken);
        var todayTransactions = await transactions.CountByDateAsync(today, cancellationToken);
        var totalPayments = await transactions.CountPaymentsAsync(cancellationToken);
        var todayPayments = await transactions.CountPaymentsByDateAsync(today, cancellationToken);

        var activeClients = await users.CountActiveClientsAsync(cancellationToken);
        var inactiveClients = await users.CountInactiveClientsAsync(cancellationToken);

        var activeSavingsAccounts = (await accounts.GetPagedAsync(
            new PagedRequest(1, 1),
            status: SavingsAccountStatus.Active,
            cancellationToken: cancellationToken)).TotalRecords;

        var activeLoans = await loans.CountActiveLoansAsync(cancellationToken);

        var activeCreditCards = (await creditCards.SearchAsync(
            1,
            1,
            status: CreditCardStatusFilter.Active,
            cancellationToken: cancellationToken)).TotalRecords;

        var totalProducts = activeSavingsAccounts + activeLoans + activeCreditCards;

        var averageDebt = await customerDebt.GetAverageActiveClientDebtAsync(
            cancellationToken);

        return new AdminDashboardDto(
            totalTransactions,
            todayTransactions,
            totalPayments,
            todayPayments,
            activeClients,
            inactiveClients,
            totalProducts,
            activeLoans,
            activeCreditCards,
            activeSavingsAccounts,
            averageDebt);
    }
}
