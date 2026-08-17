using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.CreditCards;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class CreditCardPaymentsController(
    ICardPaymentService cardPaymentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new CreditCardPaymentViewModel
        {
            OperationId = Guid.NewGuid()
        };
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreditCardPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await cardPaymentService.ProcessPaymentAsync(
                new CreditCardPaymentRequest(
                    model.CreditCardId,
                    model.SourceAccountId,
                    model.Amount,
                    model.OperationId),
                cancellationToken);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
                await LoadOptionsAsync(model, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = result.HasNotificationWarning
                ? "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación."
                : $"El pago de RD$ {result.Value.EffectiveAmount:N2} fue aplicado correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            await LoadOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    private async Task LoadOptionsAsync(
        CreditCardPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        var options = await cardPaymentService.GetClientOptionsAsync(
            cancellationToken);
        model.CreditCards = options.CreditCards;
        model.SavingsAccounts = options.SavingsAccounts;
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
