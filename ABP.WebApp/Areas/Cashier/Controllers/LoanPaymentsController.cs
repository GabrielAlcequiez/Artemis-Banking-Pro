using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Cashier.ViewModels.Loans;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = nameof(Roles.Cashier))]
public sealed class LoanPaymentsController(
    ILoanPaymentService loanPaymentService) : Controller
{
    [HttpGet]
    public IActionResult Create() =>
        View(new CashierLoanPaymentViewModel
        {
            OperationId = Guid.NewGuid()
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        CashierLoanPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await loanPaymentService.PrepareCashierPaymentAsync(
            model.SourceAccountNumber,
            model.LoanNumber,
            model.Amount,
            model.OperationId,
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return View("Create", model);
        }

        var preview = result.Value;

        return View(new CashierLoanPaymentConfirmationViewModel
        {
            LoanId = preview.LoanId,
            SourceAccountId = preview.SourceAccountId,
            OperationId = preview.OperationId,
            AccountOwnerFullName = preview.AccountOwnerFullName,
            AccountNumber = preview.AccountNumber,
            LoanOwnerFullName = preview.LoanOwnerFullName,
            LoanNumber = preview.LoanNumber,
            RequestedAmount = preview.RequestedAmount,
            EffectiveAmount = preview.EffectiveAmount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(
        CashierLoanPaymentConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await loanPaymentService.ProcessPaymentAsync(
                new LoanPaymentRequest(
                    model.LoanId,
                    model.SourceAccountId,
                    model.RequestedAmount,
                    model.OperationId),
                cancellationToken);

            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error.Description;
                return RedirectToAction(nameof(Create));
            }

            TempData["SuccessMessage"] = result.HasNotificationWarning
                ? "El pago fue realizado correctamente, pero no fue posible enviar una o más notificaciones por correo."
                : result.Value.IsCompleted
                    ? $"El pago de RD$ {result.Value.EffectiveAmount:N2} fue aplicado y el préstamo quedó saldado."
                    : $"El pago de RD$ {result.Value.EffectiveAmount:N2} fue aplicado correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Cashier" });
        }
        catch (ValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Errors.First().ErrorMessage;
            return RedirectToAction(nameof(Create));
        }
    }
}
