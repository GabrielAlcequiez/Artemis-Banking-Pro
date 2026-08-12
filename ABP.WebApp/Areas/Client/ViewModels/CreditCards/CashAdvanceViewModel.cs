using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Rules.Cards;

namespace ABP.WebApp.Areas.Client.ViewModels.CreditCards;

public sealed class CashAdvanceViewModel
{
    public Guid CreditCardId { get; set; }

    public Guid TargetAccountId { get; set; }

    public decimal Amount { get; set; }

    public Guid OperationId { get; set; }

    public decimal Interest =>
        CreditCardRules.CalculateCashAdvanceInterest(Amount);

    public decimal TotalCharge =>
        CreditCardRules.CalculateCashAdvanceTotal(Amount);

    public IReadOnlyCollection<CreditCardOperationOptionDto> CreditCards { get; set; } =
        Array.Empty<CreditCardOperationOptionDto>();

    public IReadOnlyCollection<SavingsAccountOperationOptionDto> SavingsAccounts { get; set; } =
        Array.Empty<SavingsAccountOperationOptionDto>();
}
