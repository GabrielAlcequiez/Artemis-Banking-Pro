using ABP.Domain.Enums;

namespace ABP.Application.Features.Dashboards.DTOs;

public sealed record ClientSavingsAccountPortfolioItemDto(
    Guid Id,
    string AccountNumber,
    decimal Balance,
    SavingsAccountType Type);
