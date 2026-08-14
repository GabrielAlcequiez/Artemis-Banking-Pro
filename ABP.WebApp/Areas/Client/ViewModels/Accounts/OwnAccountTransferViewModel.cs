using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Accounts;

public sealed class OwnAccountTransferViewModel
{
    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public IReadOnlyCollection<SavingsAccountOperationOptionDto> Accounts { get; set; } =
        Array.Empty<SavingsAccountOperationOptionDto>();
}
