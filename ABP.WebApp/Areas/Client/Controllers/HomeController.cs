using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.WebApp.Areas.Client.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class HomeController(
        ICreditCardService creditCardService) : Controller
    {
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var cards = await creditCardService.GetClientActiveCardsAsync(
                cancellationToken);

            return View(new ClientHomeViewModel
            {
                CreditCards = cards
            });
        }
    }
}
