using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Cashier.ViewModels.Accounts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = nameof(Roles.Cashier))]
public sealed class TransactionsController(
    IMoneyTransferService moneyTransferService,
    ICashierAccountOperationService cashierAccountOperationService,
    ICurrentUserService currentUser,
    IValidator<TransferFundsRequest> transferValidator) : Controller
{
    [HttpGet]
    public IActionResult Deposit() =>
        View(new CashierDepositViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDeposit(
        CashierDepositViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await cashierAccountOperationService.PrepareDepositAsync(
            model.AccountNumber, model.Amount, cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return View("Deposit", model);
        }

        var preview = result.Value;

        return View("ConfirmDeposit", new CashierDepositConfirmationViewModel
        {
            AccountNumber = preview.AccountNumber,
            AccountOwnerFullName = preview.AccountOwnerFullName,
            Amount = preview.Amount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteDeposit(
        CashierDepositConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        var request = new DepositRequest
        {
            DestinationAccountNumber = model.AccountNumber,
            Amount = model.Amount,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Cashier)
        };

        var result = await moneyTransferService.DepositAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Deposit));
        }

        TempData["SuccessMessage"] =
            $"El depósito de RD$ {result.Value.EffectiveAmount:N2} fue aplicado correctamente.";
        return RedirectToAction("Index", "Home", new { area = "Cashier" });
    }

    [HttpGet]
    public IActionResult Withdrawal() =>
        View(new CashierWithdrawalViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmWithdrawal(
        CashierWithdrawalViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await cashierAccountOperationService.PrepareWithdrawalAsync(
            model.AccountNumber, model.Amount, cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return View("Withdrawal", model);
        }

        var preview = result.Value;

        return View("ConfirmWithdrawal", new CashierWithdrawalConfirmationViewModel
        {
            AccountNumber = preview.AccountNumber,
            AccountOwnerFullName = preview.AccountOwnerFullName,
            AvailableBalance = preview.AvailableBalance,
            Amount = preview.Amount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteWithdrawal(
        CashierWithdrawalConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        var request = new WithdrawalRequest
        {
            SourceAccountNumber = model.AccountNumber,
            Amount = model.Amount,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Cashier)
        };

        var result = await moneyTransferService.WithdrawAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Withdrawal));
        }

        TempData["SuccessMessage"] =
            $"El retiro de RD$ {result.Value.EffectiveAmount:N2} fue aplicado correctamente.";
        return RedirectToAction("Index", "Home", new { area = "Cashier" });
    }

    [HttpGet]
    public IActionResult ThirdPartyTransfer() =>
        View(new CashierThirdPartyTransferViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmThirdPartyTransfer(
        CashierThirdPartyTransferViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await cashierAccountOperationService.PrepareThirdPartyTransferAsync(
            model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount, cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return View("ThirdPartyTransfer", model);
        }

        var preview = result.Value;

        return View("ConfirmThirdPartyTransfer", new CashierThirdPartyTransferConfirmationViewModel
        {
            SourceAccountId = preview.SourceAccountId,
            SourceAccountNumber = preview.SourceAccountNumber,
            SourceOwnerFullName = preview.SourceOwnerFullName,
            DestinationAccountId = preview.DestinationAccountId,
            DestinationAccountNumber = preview.DestinationAccountNumber,
            DestinationOwnerFullName = preview.DestinationOwnerFullName,
            Amount = preview.Amount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteThirdPartyTransfer(
        CashierThirdPartyTransferConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new TransferFundsRequest
            {
                SourceAccountId = model.SourceAccountId,
                DestinationAccountId = model.DestinationAccountId,
                Amount = model.Amount,
                OperationType = FinancialOperationType.ExpressTransfer,
                ActorUserId = currentUser.UserId ?? string.Empty,
                ActorRole = nameof(Roles.Cashier)
            };

            await transferValidator.ValidateAndThrowAsync(request, cancellationToken);

            var result = await moneyTransferService.TransferAsync(request, cancellationToken);

            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error.Description;
                return RedirectToAction(nameof(ThirdPartyTransfer));
            }

            TempData["SuccessMessage"] =
                $"La transferencia de RD$ {result.Value.EffectiveAmount:N2} fue aplicada correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Cashier" });
        }
        catch (ValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Errors.First().ErrorMessage;
            return RedirectToAction(nameof(ThirdPartyTransfer));
        }
    }
}
