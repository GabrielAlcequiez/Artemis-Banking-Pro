namespace ABP.Application.Features.Dashboards.DTOs;

public sealed record AdminDashboardDto(
    int TotalTransactions,
    int TodayTransactions,
    int TotalPayments,
    int TodayPayments,
    int ActiveClients,
    int InactiveClients,
    int TotalFinancialProducts,
    int ActiveLoans,
    int ActiveCreditCards,
    int ActiveSavingsAccounts,
    decimal AverageDebtPerClient);
