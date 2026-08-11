using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.CreditCards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class CreditCardsController(
    ICreditCardService creditCardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var card = await creditCardService.GetClientDetailAsync(
            id,
            cancellationToken);

        return card is null
            ? NotFound()
            : View(new CreditCardDetailViewModel { Card = card });
    }
}
