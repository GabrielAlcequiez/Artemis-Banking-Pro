using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class SavingsAccountsController(
    ISavingsAccountQueryService accountQueryService,
    IAccountClientSelectionService clientSelectionService,
    ISavingsAccountAdminService adminService,
    ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? ownerIdentification = null,
        string? status = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(status, out var statusFilter))
        {
            ModelState.AddModelError(nameof(status), "El estado debe ser activa, cancelada o todas.");
        }

        if (!TryParseType(type, out var typeFilter))
        {
            ModelState.AddModelError(nameof(type), "El tipo debe ser principal, secundaria o todas.");
        }

        var model = new SavingsAccountIndexViewModel
        {
            Page = page,
            PageSize = pageSize,
            OwnerIdentification = ownerIdentification,
            Status = status,
            Type = type
        };

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Result = await accountQueryService.ListAsync(
            new PagedRequest(page, pageSize),
            ownerIdentification,
            statusFilter,
            typeFilter,
            cancellationToken);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await accountQueryService.GetDetailAsync(id, cancellationToken);

        return account is null
            ? NotFound()
            : View(new SavingsAccountDetailViewModel { Account = account });
    }

    [HttpGet]
    public async Task<IActionResult> SelectClient(
        int page = 1,
        int pageSize = 20,
        string? identification = null,
        CancellationToken cancellationToken = default)
    {
        var model = new AccountClientSelectionViewModel
        {
            Page = page,
            PageSize = pageSize,
            Identification = identification
        };

        await PopulateClientSelectionAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectClient(
        AccountClientSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var client = await clientSelectionService.GetActiveClientAsync(
                model.SelectedClientId!,
                cancellationToken);

            if (client is not null)
            {
                return RedirectToAction(nameof(Create), new { clientId = client.Id });
            }

            ModelState.AddModelError(
                nameof(model.SelectedClientId),
                "El cliente seleccionado no existe o ya no está activo.");
        }

        await PopulateClientSelectionAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string clientId,
        CancellationToken cancellationToken)
    {
        var client = await clientSelectionService.GetActiveClientAsync(
            clientId,
            cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return View(ToCreateViewModel(client));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSecondaryAccountViewModel model,
        CancellationToken cancellationToken)
    {
        var client = await clientSelectionService.GetActiveClientAsync(
            model.ClientId,
            cancellationToken);

        if (client is null)
        {
            ModelState.AddModelError(
                nameof(model.ClientId),
                "El cliente seleccionado no existe o ya no está activo.");
        }
        else
        {
            CopyClientPresentation(client, model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await adminService.CreateSecondaryAccountAsync(
                new CreateSecondaryAccountRequest
                {
                    OwnerUserId = model.ClientId,
                    InitialBalance = model.InitialBalance,
                    ActorUserId = currentUser.UserId ?? string.Empty,
                    ActorRole = nameof(Roles.Administrator)
                },
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(model);
            }

            TempData[SuccessMessageKey] = "La cuenta secundaria fue creada correctamente.";
            return RedirectToAction(nameof(Details), new { id = result.Value });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmCancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await accountQueryService.GetDetailAsync(id, cancellationToken);

        if (account is null)
        {
            return NotFound();
        }

        if (account.Type == SavingsAccountType.Principal)
        {
            TempData[ErrorMessageKey] = "La cuenta principal no se puede cancelar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (account.Status == SavingsAccountStatus.Cancelled)
        {
            TempData[ErrorMessageKey] = "La cuenta ya está cancelada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new CancelSavingsAccountViewModel
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        CancelSavingsAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCancelPresentationAsync(model, cancellationToken);
            return View(nameof(ConfirmCancel), model);
        }

        try
        {
            var result = await adminService.CancelAsync(
                new CancelSavingsAccountRequest
                {
                    AccountId = model.AccountId,
                    ActorUserId = currentUser.UserId ?? string.Empty,
                    ActorRole = nameof(Roles.Administrator)
                },
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                await PopulateCancelPresentationAsync(model, cancellationToken);
                return View(nameof(ConfirmCancel), model);
            }

            TempData[SuccessMessageKey] = "La cuenta fue cancelada correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.AccountId });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            await PopulateCancelPresentationAsync(model, cancellationToken);
            return View(nameof(ConfirmCancel), model);
        }
    }

    private async Task PopulateClientSelectionAsync(
        AccountClientSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            model.Result = await clientSelectionService.SearchAsync(
                new AccountClientSearchRequest(
                    model.Page,
                    model.PageSize,
                    model.Identification),
                cancellationToken);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
        }
    }

    private async Task PopulateCancelPresentationAsync(
        CancelSavingsAccountViewModel model,
        CancellationToken cancellationToken)
    {
        var account = await accountQueryService.GetDetailAsync(
            model.AccountId,
            cancellationToken);

        if (account is not null)
        {
            model.AccountNumber = account.AccountNumber;
        }
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }

    private void AddOperationError(Error error) =>
        ModelState.AddModelError(string.Empty, ToSpanishMessage(error));

    private static string ToSpanishMessage(Error error) => error switch
    {
        _ when error == AccountErrors.NotFound =>
            "La cuenta seleccionada no existe.",
        _ when error == AccountErrors.InvalidAmount =>
            "El monto debe ser mayor que cero.",
        _ when error == AccountErrors.InsufficientFunds =>
            "La cuenta no tiene fondos suficientes para esta operación.",
        _ when error == AccountErrors.InactiveAccount =>
            "La cuenta no se encuentra activa.",
        _ when error == AccountErrors.SameAccount =>
            "La cuenta de origen y destino deben ser diferentes.",
        _ when error == AccountErrors.CannotAddSelf =>
            "No puedes agregar tu propia cuenta como beneficiario.",
        _ when error == AccountErrors.BeneficiaryAlreadyExists =>
            "Esta cuenta ya está registrada como beneficiario.",
        _ when error == AccountErrors.BeneficiaryNotFound =>
            "El beneficiario indicado no existe.",
        _ when error == AccountErrors.CannotCancelPrincipal =>
            "La cuenta principal no se puede cancelar.",
        _ when error == AccountErrors.AlreadyCancelled =>
            "La cuenta ya se encuentra cancelada.",
        _ when error == AccountErrors.PrincipalAlreadyExists =>
            "El cliente ya tiene una cuenta principal.",
        _ when error == AccountErrors.PrincipalNotFound =>
            "El saldo no pudo transferirse porque el cliente no tiene cuenta principal.",
        _ => "Ocurrió un error inesperado."
    };

    private static CreateSecondaryAccountViewModel ToCreateViewModel(
        AccountClientCandidateDto client) => new()
        {
            ClientId = client.Id,
            ClientFullName = client.FullName,
            ClientIdentification = client.Identification,
            ClientEmail = client.Email
        };

    private static void CopyClientPresentation(
        AccountClientCandidateDto client,
        CreateSecondaryAccountViewModel model)
    {
        model.ClientFullName = client.FullName;
        model.ClientIdentification = client.Identification;
        model.ClientEmail = client.Email;
    }

    private static bool TryParseStatus(
        string? value,
        out SavingsAccountStatus? status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null or "":
            case "todas":
                status = null;
                return true;
            case "activa":
                status = SavingsAccountStatus.Active;
                return true;
            case "cancelada":
                status = SavingsAccountStatus.Cancelled;
                return true;
            default:
                status = null;
                return false;
        }
    }

    private static bool TryParseType(
        string? value,
        out SavingsAccountType? type)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null or "":
            case "todas":
                type = null;
                return true;
            case "principal":
                type = SavingsAccountType.Principal;
                return true;
            case "secundaria":
                type = SavingsAccountType.Secondary;
                return true;
            default:
                type = null;
                return false;
        }
    }

    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
}
