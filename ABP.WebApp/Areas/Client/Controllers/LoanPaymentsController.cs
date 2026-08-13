using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.Loans;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class LoanPaymentsController(
    ILoanPaymentService loanPaymentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new LoanPaymentViewModel
        {
            OperationId = Guid.NewGuid()
        };
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LoanPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await loanPaymentService.ProcessPaymentAsync(
                new LoanPaymentRequest(
                    model.LoanId,
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

            TempData["SuccessMessage"] = result.Value.IsCompleted
                ? $"El pago de RD$ {result.Value.EffectiveAmount:N2} fue aplicado y el préstamo quedó saldado."
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
        LoanPaymentViewModel model,
        CancellationToken cancellationToken)
    {
        var options = await loanPaymentService.GetClientOptionsAsync(
            cancellationToken);
        model.Loans = options.Loans;
        model.SavingsAccounts = options.SavingsAccounts;

        if (model.LoanId == Guid.Empty && model.Loans.Count == 1)
        {
            model.LoanId = model.Loans.Single().Id;
        }
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
