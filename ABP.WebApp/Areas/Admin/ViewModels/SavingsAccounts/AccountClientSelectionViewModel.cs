using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;

public sealed class AccountClientSelectionViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string? SelectedClientId { get; set; }

    public PagedResult<AccountClientCandidateDto>? Result { get; set; }
}
