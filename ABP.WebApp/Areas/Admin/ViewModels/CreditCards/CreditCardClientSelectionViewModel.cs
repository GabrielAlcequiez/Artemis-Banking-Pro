using ABP.Application.Features.CreditCards.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class CreditCardClientSelectionViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Identification { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string? SelectedClientId { get; set; }

    public CreditCardClientSelectionResult? Result { get; set; }
}
