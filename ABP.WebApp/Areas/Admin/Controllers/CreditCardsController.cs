using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.ViewModels.CreditCards;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class CreditCardsController(
    ICreditCardService creditCardService,
    ICreditCardClientSelectionService clientSelectionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? identification = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(status, out var statusFilter))
        {
            ModelState.AddModelError(nameof(status), "El estado debe ser activa, cancelada o todas.");
        }

        var model = new CreditCardIndexViewModel
        {
            Page = page,
            PageSize = pageSize,
            Identification = identification,
            Status = status
        };

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            model.Result = await creditCardService.ListAsync(
                new CreditCardListRequest(page, pageSize, identification, statusFilter),
                cancellationToken);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SelectClient(
        int page = 1,
        int pageSize = 20,
        string? identification = null,
        CancellationToken cancellationToken = default)
    {
        var model = new CreditCardClientSelectionViewModel
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
        CreditCardClientSelectionViewModel model,
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
        CreateCreditCardViewModel model,
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
            var result = await creditCardService.CreateAsync(
                new CreateCreditCardRequest(model.ClientId, model.CreditLimit),
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(model);
            }

            TempData[SuccessMessageKey] = result.HasNotificationWarning
                ? "La tarjeta fue creada correctamente, pero no fue posible enviar el correo de notificación."
                : "La tarjeta de crédito fue creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var card = await creditCardService.GetDetailAsync(id, cancellationToken);

        return card is null
            ? NotFound()
            : View(new CreditCardDetailViewModel { Card = card });
    }

    [HttpGet]
    public async Task<IActionResult> EditLimit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var card = await creditCardService.GetDetailAsync(id, cancellationToken);

        if (card is null)
        {
            return NotFound();
        }

        if (IsCancelled(card))
        {
            TempData[ErrorMessageKey] = "No se puede modificar una tarjeta cancelada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(ToEditLimitViewModel(card));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLimit(
        EditCreditLimitViewModel model,
        CancellationToken cancellationToken)
    {
        var card = await creditCardService.GetDetailAsync(
            model.CreditCardId,
            cancellationToken);

        if (card is null)
        {
            return NotFound();
        }

        CopyCardPresentation(card, model);

        if (IsCancelled(card))
        {
            ModelState.AddModelError(string.Empty, "No se puede modificar una tarjeta cancelada.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await creditCardService.UpdateLimitAsync(
                new UpdateCreditLimitRequest(model.CreditCardId, model.CreditLimit),
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(model);
            }

            TempData[SuccessMessageKey] = result.HasNotificationWarning
                ? "El límite fue actualizado correctamente, pero no fue posible enviar el correo de notificación."
                : "El límite de crédito fue actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.CreditCardId });
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
        var card = await creditCardService.GetDetailAsync(id, cancellationToken);

        if (card is null)
        {
            return NotFound();
        }

        if (IsCancelled(card))
        {
            TempData[ErrorMessageKey] = "La tarjeta ya está cancelada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new CancelCreditCardViewModel
        {
            CreditCardId = card.Id,
            LastFourDigits = card.LastFourDigits
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        CancelCreditCardViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCancelPresentationAsync(model, cancellationToken);
            return View(nameof(ConfirmCancel), model);
        }

        try
        {
            var result = await creditCardService.CancelAsync(
                new CancelCreditCardRequest(model.CreditCardId),
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                await PopulateCancelPresentationAsync(model, cancellationToken);
                return View(nameof(ConfirmCancel), model);
            }

            TempData[SuccessMessageKey] = "La tarjeta de crédito fue cancelada correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.CreditCardId });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            await PopulateCancelPresentationAsync(model, cancellationToken);
            return View(nameof(ConfirmCancel), model);
        }
    }

    private async Task PopulateClientSelectionAsync(
        CreditCardClientSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            model.Result = await clientSelectionService.SearchAsync(
                new CreditCardClientSearchRequest(
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
        CancelCreditCardViewModel model,
        CancellationToken cancellationToken)
    {
        var card = await creditCardService.GetDetailAsync(
            model.CreditCardId,
            cancellationToken);

        if (card is not null)
        {
            model.LastFourDigits = card.LastFourDigits;
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
        _ when error == CreditCardErrors.ClientNotFound =>
            "El cliente indicado no existe.",
        _ when error == CreditCardErrors.ClientInactive || error == CreditCardErrors.ClientNotEligible =>
            "Solo se puede asignar tarjetas de crédito a clientes activos.",
        _ when error == CreditCardErrors.NotFound =>
            "La tarjeta seleccionada no existe.",
        _ when error == CreditCardErrors.Cancelled =>
            "No se puede modificar una tarjeta cancelada.",
        _ when error == CreditCardErrors.LimitBelowDebt =>
            "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.",
        _ when error == CreditCardErrors.OutstandingDebt =>
            "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.",
        _ when error == CreditCardErrors.NumberGenerationFailed =>
            "No fue posible generar un número de tarjeta único.",
        _ when error == CreditCardErrors.AdministratorRequired =>
            "Se requiere un administrador autenticado.",
        _ => "Ocurrió un error inesperado."
    };

    private static CreateCreditCardViewModel ToCreateViewModel(
        CreditCardClientCandidateDto client) => new()
        {
            ClientId = client.Id,
            ClientFullName = client.FullName,
            ClientIdentification = client.Identification,
            ClientEmail = client.Email
        };

    private static void CopyClientPresentation(
        CreditCardClientCandidateDto client,
        CreateCreditCardViewModel model)
    {
        model.ClientFullName = client.FullName;
        model.ClientIdentification = client.Identification;
        model.ClientEmail = client.Email;
    }

    private static EditCreditLimitViewModel ToEditLimitViewModel(
        CreditCardDetailDto card) => new()
        {
            CreditCardId = card.Id,
            MaskedCardNumber = card.MaskedCardNumber,
            CurrentDebt = card.CurrentDebt,
            CreditLimit = card.CreditLimit
        };

    private static void CopyCardPresentation(
        CreditCardDetailDto card,
        EditCreditLimitViewModel model)
    {
        model.MaskedCardNumber = card.MaskedCardNumber;
        model.CurrentDebt = card.CurrentDebt;
    }

    private static bool IsCancelled(CreditCardDetailDto card) =>
        string.Equals(card.Status, "Cancelada", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseStatus(
        string? value,
        out CreditCardStatusFilter? status)
    {
        status = value?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "activa" => CreditCardStatusFilter.Active,
            "cancelada" => CreditCardStatusFilter.Cancelled,
            "todas" => CreditCardStatusFilter.All,
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) || status.HasValue;
    }

    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
}
