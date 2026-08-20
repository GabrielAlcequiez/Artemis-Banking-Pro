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
public sealed class TransactionsController(
    IMoneyTransferService moneyTransferService,
    IBeneficiaryService beneficiaryService,
    IClientAccountOptionsService accountOptionsService,
    ICurrentUserService currentUser,
    IValidator<TransferFundsRequest> transferValidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Express(
        CancellationToken cancellationToken)
    {
        var model = new TransferViewModel();
        await LoadSourceAccountsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Express(
        TransferViewModel model,
        CancellationToken cancellationToken)
    {
        await LoadSourceAccountsAsync(model, cancellationToken);

        if (!IsOwnAccount(model, model.SourceAccountId))
        {
            ModelState.AddModelError(
                nameof(model.SourceAccountId),
                "La cuenta de origen seleccionada no es válida.");
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
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount,
                OperationType = FinancialOperationType.ExpressTransfer,
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
                $"La transferencia express de RD$ {result.Value.EffectiveAmount:N2} fue aplicada correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Beneficiary(
        CancellationToken cancellationToken)
    {
        var model = new TransferViewModel();
        await LoadSourceAccountsAsync(model, cancellationToken);
        await LoadBeneficiariesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Beneficiary(
        TransferViewModel model,
        CancellationToken cancellationToken)
    {
        await LoadSourceAccountsAsync(model, cancellationToken);
        await LoadBeneficiariesAsync(model, cancellationToken);

        if (!IsOwnAccount(model, model.SourceAccountId))
        {
            ModelState.AddModelError(
                nameof(model.SourceAccountId),
                "La cuenta de origen seleccionada no es válida.");
        }

        var beneficiary = model.Beneficiaries.FirstOrDefault(b => b.Id == model.BeneficiaryId);
        if (beneficiary is null)
        {
            ModelState.AddModelError(
                nameof(model.BeneficiaryId),
                "Debe seleccionar un beneficiario válido.");
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
                DestinationAccountId = beneficiary!.BeneficiaryAccountId,
                Amount = model.Amount,
                OperationType = FinancialOperationType.BeneficiaryTransfer,
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
                $"La transferencia de RD$ {result.Value.EffectiveAmount:N2} a {beneficiary.BeneficiaryOwnerName} fue aplicada correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    private async Task LoadSourceAccountsAsync(
        TransferViewModel model, CancellationToken cancellationToken)
    {
        model.SourceAccounts = await accountOptionsService.GetMyActiveAccountsAsync(cancellationToken);
    }

    private async Task LoadBeneficiariesAsync(
        TransferViewModel model, CancellationToken cancellationToken)
    {
        model.Beneficiaries = await beneficiaryService.ListAsync(
            currentUser.UserId ?? string.Empty, cancellationToken);
    }

    private static bool IsOwnAccount(TransferViewModel model, Guid accountId) =>
        model.SourceAccounts.Any(account => account.Id == accountId);

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
