using System.ComponentModel.DataAnnotations;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.WebApp.Areas.Admin.ViewModels.Loans;

public sealed class LoanClientSelectionViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string? SelectedClientId { get; set; }

    public LoanClientSelectionResult? Result { get; set; }
}
