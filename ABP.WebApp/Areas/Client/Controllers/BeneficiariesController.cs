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
public sealed class BeneficiariesController(
    IBeneficiaryService beneficiaryService,
    ICurrentUserService currentUser,
    IValidator<AddBeneficiaryRequest> addValidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new BeneficiaryIndexViewModel();
        await LoadBeneficiariesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        BeneficiaryIndexViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AddBeneficiaryRequest
            {
                OwnerUserId = currentUser.UserId ?? string.Empty,
                BeneficiaryAccountNumber = model.BeneficiaryAccountNumber ?? string.Empty
            };

            await addValidator.ValidateAndThrowAsync(request, cancellationToken);

            var result = await beneficiaryService.AddAsync(request, cancellationToken);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
                await LoadBeneficiariesAsync(model, cancellationToken);
                return View(nameof(Index), model);
            }

            TempData["SuccessMessage"] =
                $"Se agregó a {result.Value.BeneficiaryOwnerName} como beneficiario.";
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            await LoadBeneficiariesAsync(model, cancellationToken);
            return View(nameof(Index), model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        Guid beneficiaryId,
        CancellationToken cancellationToken)
    {
        var result = await beneficiaryService.RemoveAsync(
            currentUser.UserId ?? string.Empty, beneficiaryId, cancellationToken);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "El beneficiario fue eliminado correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error.Description;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadBeneficiariesAsync(
        BeneficiaryIndexViewModel model, CancellationToken cancellationToken)
    {
        model.Beneficiaries = await beneficiaryService.ListAsync(
            currentUser.UserId ?? string.Empty, cancellationToken);
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
