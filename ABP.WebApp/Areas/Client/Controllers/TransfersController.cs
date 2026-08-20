using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.Accounts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class TransfersController(
    IMoneyTransferService moneyTransferService,
    IClientAccountOptionsService accountOptionsService,
    ICurrentUserService currentUser,
    IValidator<TransferFundsRequest> transferValidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new OwnAccountTransferViewModel();
        await LoadAccountsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        OwnAccountTransferViewModel model,
        CancellationToken cancellationToken)
    {
        await LoadAccountsAsync(model, cancellationToken);

        if (!IsOwnAccount(model, model.SourceAccountId))
        {
            ModelState.AddModelError(
                nameof(model.SourceAccountId),
                "La cuenta de origen seleccionada no es válida.");
        }

        if (!IsOwnAccount(model, model.DestinationAccountId))
        {
            ModelState.AddModelError(
                nameof(model.DestinationAccountId),
                "La cuenta de destino seleccionada no es válida.");
        }

        if (model.SourceAccountId != Guid.Empty && model.SourceAccountId == model.DestinationAccountId)
        {
            ModelState.AddModelError(
                nameof(model.DestinationAccountId),
                "La cuenta de destino debe ser diferente a la de origen.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new TransferFundsRequest
            {
                SourceAccountId = model.SourceAccountId,
                DestinationAccountId = model.DestinationAccountId,
                Amount = model.Amount,
                OperationType = FinancialOperationType.OwnAccountTransfer,
                ActorUserId = currentUser.UserId ?? string.Empty,
                ActorRole = nameof(Roles.Client)
            };

            await transferValidator.ValidateAndThrowAsync(request, cancellationToken);

            var result = await moneyTransferService.TransferAsync(request, cancellationToken);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
                return View(model);
            }

            TempData["SuccessMessage"] =
                $"La transferencia de RD$ {result.Value.EffectiveAmount:N2} entre tus cuentas fue aplicada correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    private async Task LoadAccountsAsync(
        OwnAccountTransferViewModel model, CancellationToken cancellationToken)
    {
        model.Accounts = await accountOptionsService.GetMyActiveAccountsAsync(cancellationToken);
    }

    private static bool IsOwnAccount(OwnAccountTransferViewModel model, Guid accountId) =>
        model.Accounts.Any(account => account.Id == accountId);

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
