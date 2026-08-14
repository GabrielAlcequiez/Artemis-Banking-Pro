using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Accounts;

public sealed class TransferViewModel
{
    public Guid SourceAccountId { get; set; }

    public string? DestinationAccountNumber { get; set; }

    public Guid? BeneficiaryId { get; set; }

    public decimal Amount { get; set; }

    public IReadOnlyCollection<SavingsAccountOperationOptionDto> SourceAccounts { get; set; } =
        Array.Empty<SavingsAccountOperationOptionDto>();

    public IReadOnlyCollection<BeneficiaryDto> Beneficiaries { get; set; } =
        Array.Empty<BeneficiaryDto>();
}
