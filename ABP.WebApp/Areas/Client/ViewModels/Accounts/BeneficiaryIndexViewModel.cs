using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Accounts;

public sealed class BeneficiaryIndexViewModel
{
    public IReadOnlyCollection<BeneficiaryDto> Beneficiaries { get; set; } =
        Array.Empty<BeneficiaryDto>();

    public string? BeneficiaryAccountNumber { get; set; }
}
