using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.ViewModels.Loans;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class LoansController(
    ILoanService loanService,
    ILoanClientSelectionService clientSelectionService,
    ILoanOriginationService originationService,
    ILoanRateService loanRateService) : Controller
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
            ModelState.AddModelError(
                nameof(status),
                "El estado debe ser activo, completado o todos.");
        }

        var model = new LoanIndexViewModel
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
            model.Result = await loanService.ListAsync(
                new LoanListRequest(
                    page,
                    pageSize,
                    identification,
                    statusFilter),
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
        var model = new LoanClientSelectionViewModel
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
        LoanClientSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var client = await clientSelectionService.GetEligibleClientAsync(
                model.SelectedClientId!,
                cancellationToken);

            if (client is not null)
            {
                return RedirectToAction(
                    nameof(Create),
                    new { clientId = client.Id });
            }

            ModelState.AddModelError(
                nameof(model.SelectedClientId),
                "El cliente seleccionado no existe, no está activo o ya posee un préstamo activo.");
        }

        await PopulateClientSelectionAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string clientId,
        CancellationToken cancellationToken)
    {
        var client = await clientSelectionService.GetEligibleClientAsync(
            clientId,
            cancellationToken);

        return client is null
            ? NotFound()
            : View(ToCreateViewModel(client));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateLoanViewModel model,
        CancellationToken cancellationToken)
    {
        var client = await clientSelectionService.GetEligibleClientAsync(
            model.ClientId,
            cancellationToken);

        if (client is null)
        {
            ModelState.AddModelError(
                nameof(model.ClientId),
                "El cliente seleccionado no existe, no está activo o ya posee un préstamo activo.");
        }
        else
        {
            CopyClientPresentation(client, model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = ToCreateRequest(model, confirmHighRisk: false);

        try
        {
            var assessmentResult = await originationService.AssessRiskAsync(
                request,
                cancellationToken);

            if (assessmentResult.IsFailure)
            {
                AddOperationError(assessmentResult.Error);
                return View(model);
            }

            if (assessmentResult.Value.RequiresConfirmation)
            {
                return View(
                    nameof(RiskWarning),
                    ToRiskWarningViewModel(
                        model,
                        assessmentResult.Value));
            }

            var result = await originationService.CreateAsync(
                request,
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(model);
            }

            TempData[SuccessMessageKey] =
                "El préstamo fue creado y desembolsado correctamente.";
            return RedirectToAction(
                nameof(Details),
                new { id = result.Value.Id });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult RiskWarning() =>
        RedirectToAction(nameof(SelectClient));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAssignment(
        LoanRiskWarningViewModel model,
        CancellationToken cancellationToken)
    {
        var client = await clientSelectionService.GetEligibleClientAsync(
            model.ClientId,
            cancellationToken);

        if (client is null)
        {
            ModelState.AddModelError(
                nameof(model.ClientId),
                "El cliente seleccionado ya no es elegible para recibir el préstamo.");
            return View(nameof(RiskWarning), model);
        }

        model.ClientFullName = client.FullName;
        model.ClientIdentification = client.Identification;

        try
        {
            var presentationAssessment = await originationService.AssessRiskAsync(
                ToCreateRequest(model, confirmHighRisk: false),
                cancellationToken);

            if (presentationAssessment.IsFailure)
            {
                AddOperationError(presentationAssessment.Error);
                return View(nameof(RiskWarning), model);
            }

            CopyRiskPresentation(presentationAssessment.Value, model);

            var result = await originationService.CreateAsync(
                ToCreateRequest(model, confirmHighRisk: true),
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(nameof(RiskWarning), model);
            }

            TempData[SuccessMessageKey] =
                "El préstamo de alto riesgo fue confirmado, creado y desembolsado correctamente.";
            return RedirectToAction(
                nameof(Details),
                new { id = result.Value.Id });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(nameof(RiskWarning), model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var loan = await loanService.GetDetailAsync(
            id,
            cancellationToken);

        return loan is null
            ? NotFound()
            : View(new LoanDetailViewModel { Loan = loan });
    }

    [HttpGet]
    public async Task<IActionResult> EditRate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var loan = await loanService.GetDetailAsync(
            id,
            cancellationToken);

        if (loan is null)
        {
            return NotFound();
        }

        if (!IsActive(loan))
        {
            TempData[ErrorMessageKey] =
                "No se puede modificar la tasa de un préstamo completado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(ToEditRateViewModel(loan));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRate(
        EditLoanRateViewModel model,
        CancellationToken cancellationToken)
    {
        var loan = await loanService.GetDetailAsync(
            model.LoanId,
            cancellationToken);

        if (loan is null)
        {
            return NotFound();
        }

        CopyLoanPresentation(loan, model);

        if (!IsActive(loan))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se puede modificar la tasa de un préstamo completado.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await loanRateService.UpdateRateAsync(
                new UpdateLoanRateRequest(
                    model.LoanId,
                    model.AnnualInterestRate),
                cancellationToken);

            if (result.IsFailure)
            {
                AddOperationError(result.Error);
                return View(model);
            }

            TempData[SuccessMessageKey] =
                "La tasa del préstamo fue actualizada correctamente.";
            return RedirectToAction(
                nameof(Details),
                new { id = model.LoanId });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    private async Task PopulateClientSelectionAsync(
        LoanClientSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            model.Result = await clientSelectionService.SearchAsync(
                new LoanClientSearchRequest(
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

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(
                error.PropertyName,
                error.ErrorMessage);
        }
    }

    private void AddOperationError(Error error) =>
        ModelState.AddModelError(
            string.Empty,
            ToSpanishMessage(error));

    private static string ToSpanishMessage(Error error) => error switch
    {
        _ when error == LoanErrors.ClientNotFound =>
            "El cliente indicado no existe.",
        _ when error == LoanErrors.ClientInactive =>
            "Solo se puede asignar préstamos a clientes activos.",
        _ when error == LoanErrors.ActiveLoanExists =>
            "El cliente ya posee un préstamo activo.",
        _ when error == LoanErrors.NotFound =>
            "El préstamo seleccionado no existe.",
        _ when error == LoanErrors.Inactive =>
            "El préstamo debe estar activo para realizar esta operación.",
        _ when error == LoanErrors.HighRiskConfirmationRequired =>
            "Debe confirmar explícitamente la asignación del préstamo de alto riesgo.",
        _ when error == LoanErrors.NoFuturePendingInstallments =>
            LoanErrors.NoFuturePendingInstallments.Description,
        _ when error == LoanErrors.NumberGenerationFailed =>
            LoanErrors.NumberGenerationFailed.Description,
        _ when error == LoanErrors.AdministratorRequired =>
            "Se requiere un administrador autenticado.",
        _ when error == LoanErrors.PrincipalAccountNotFound =>
            LoanErrors.PrincipalAccountNotFound.Description,
        _ when error == LoanErrors.ConcurrencyConflict =>
            LoanErrors.ConcurrencyConflict.Description,
        _ => "Ocurrió un error inesperado."
    };

    private static CreateLoanViewModel ToCreateViewModel(
        LoanClientCandidateDto client) => new()
        {
            ClientId = client.Id,
            ClientFullName = client.FullName,
            ClientIdentification = client.Identification,
            ClientEmail = client.Email,
            CurrentDebt = client.CurrentDebt
        };

    private static void CopyClientPresentation(
        LoanClientCandidateDto client,
        CreateLoanViewModel model)
    {
        model.ClientFullName = client.FullName;
        model.ClientIdentification = client.Identification;
        model.ClientEmail = client.Email;
        model.CurrentDebt = client.CurrentDebt;
    }

    private static CreateLoanRequest ToCreateRequest(
        CreateLoanViewModel model,
        bool confirmHighRisk) =>
        new(
            model.ClientId,
            model.CapitalAmount,
            model.TermInMonths,
            model.AnnualInterestRate,
            confirmHighRisk);

    private static CreateLoanRequest ToCreateRequest(
        LoanRiskWarningViewModel model,
        bool confirmHighRisk) =>
        new(
            model.ClientId,
            model.CapitalAmount,
            model.TermInMonths,
            model.AnnualInterestRate,
            confirmHighRisk);

    private static LoanRiskWarningViewModel ToRiskWarningViewModel(
        CreateLoanViewModel model,
        HighRiskAssessmentDto assessment) =>
        new()
        {
            ClientId = model.ClientId,
            ClientFullName = model.ClientFullName,
            ClientIdentification = model.ClientIdentification,
            CapitalAmount = model.CapitalAmount,
            TermInMonths = model.TermInMonths,
            AnnualInterestRate = model.AnnualInterestRate,
            RiskType = assessment.RiskType,
            CurrentDebt = assessment.CurrentDebt,
            ProjectedDebt = assessment.ProjectedDebt,
            AverageDebt = assessment.AverageDebt
        };

    private static void CopyRiskPresentation(
        HighRiskAssessmentDto assessment,
        LoanRiskWarningViewModel model)
    {
        model.RiskType = assessment.RiskType;
        model.CurrentDebt = assessment.CurrentDebt;
        model.ProjectedDebt = assessment.ProjectedDebt;
        model.AverageDebt = assessment.AverageDebt;
    }

    private static EditLoanRateViewModel ToEditRateViewModel(
        LoanDetailDto loan) => new()
        {
            LoanId = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientFullName = loan.ClientFullName,
            PendingAmount = loan.PendingAmount,
            Status = loan.Status,
            CurrentAnnualInterestRate = loan.AnnualInterestRate,
            AnnualInterestRate = loan.AnnualInterestRate
        };

    private static void CopyLoanPresentation(
        LoanDetailDto loan,
        EditLoanRateViewModel model)
    {
        model.LoanNumber = loan.LoanNumber;
        model.ClientFullName = loan.ClientFullName;
        model.PendingAmount = loan.PendingAmount;
        model.Status = loan.Status;
        model.CurrentAnnualInterestRate = loan.AnnualInterestRate;
    }

    private static bool IsActive(LoanDetailDto loan) =>
        string.Equals(
            loan.Status,
            "Activo",
            StringComparison.OrdinalIgnoreCase);

    private static bool TryParseStatus(
        string? value,
        out LoanStatusFilter? status)
    {
        status = value?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "activo" => LoanStatusFilter.Active,
            "completado" => LoanStatusFilter.Completed,
            "todos" => LoanStatusFilter.All,
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) || status.HasValue;
    }

    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
}
