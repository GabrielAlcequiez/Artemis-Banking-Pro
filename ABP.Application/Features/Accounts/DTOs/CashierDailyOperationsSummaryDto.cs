namespace ABP.Application.Features.Accounts.DTOs;

public sealed record CashierDailyOperationsSummaryDto(
    int TotalTransactionsToday,
    int PaymentsToday,
    int DepositsToday,
    int WithdrawalsToday);
