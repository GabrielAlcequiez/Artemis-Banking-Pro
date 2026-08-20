using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class SavingsAccountsController(
    IClientAccountOptionsService accountOptionsService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await accountOptionsService.GetDetailAsync(id, cancellationToken);

        return account is null
            ? NotFound()
            : View(new SavingsAccountDetailViewModel { Account = account });
    }
}
