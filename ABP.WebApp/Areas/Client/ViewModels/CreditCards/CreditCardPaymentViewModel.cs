using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.CreditCards;

public sealed class CreditCardPaymentViewModel
{
    public Guid CreditCardId { get; set; }

    public Guid SourceAccountId { get; set; }

    public decimal Amount { get; set; }

    public Guid OperationId { get; set; }

    public IReadOnlyCollection<CreditCardOperationOptionDto> CreditCards { get; set; } =
        Array.Empty<CreditCardOperationOptionDto>();

    public IReadOnlyCollection<SavingsAccountOperationOptionDto> SavingsAccounts { get; set; } =
        Array.Empty<SavingsAccountOperationOptionDto>();
}
