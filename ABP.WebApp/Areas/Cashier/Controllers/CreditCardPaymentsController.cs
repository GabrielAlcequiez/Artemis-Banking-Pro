using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Cashier.ViewModels.CreditCards;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = nameof(Roles.Cashier))]
public sealed class CreditCardPaymentsController(
    ICardPaymentService cardPaymentService) : Controller
{
    [HttpGet]
    public IActionResult Create() =>
        View(new CashierCreditCardPaymentViewModel
        {
            OperationId = Guid.NewGuid()
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        CashierCreditCardPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await cardPaymentService.PrepareCashierPaymentAsync(
            model.SourceAccountNumber,
            model.CreditCardNumber,
            model.Amount,
            model.OperationId,
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return View("Create", model);
        }

        var preview = result.Value;
        return View(new CashierCreditCardPaymentConfirmationViewModel
        {
            CreditCardId = preview.CreditCardId,
            SourceAccountId = preview.SourceAccountId,
            OperationId = preview.OperationId,
            AccountOwnerFullName = preview.AccountOwnerFullName,
            AccountNumber = preview.AccountNumber,
            CardOwnerFullName = preview.CardOwnerFullName,
            CardLastFourDigits = preview.CardLastFourDigits,
            RequestedAmount = preview.RequestedAmount,
            EffectiveAmount = preview.EffectiveAmount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(
        CashierCreditCardPaymentConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await cardPaymentService.ProcessPaymentAsync(
                new CreditCardPaymentRequest(
                    model.CreditCardId,
                    model.SourceAccountId,
                    model.RequestedAmount,
                    model.OperationId),
                cancellationToken);

            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error.Description;
                return RedirectToAction(nameof(Create));
            }

            TempData["SuccessMessage"] =
                $"El pago de RD$ {result.Value.EffectiveAmount:N2} fue aplicado correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Cashier" });
        }
        catch (ValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Errors.First().ErrorMessage;
            return RedirectToAction(nameof(Create));
        }
    }
}
