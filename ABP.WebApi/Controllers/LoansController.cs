using ABP.Application.Common;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.Commands.CreateLoan;
using ABP.Application.Features.Loans.Commands.UpdateLoanRate;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Queries.AssessLoanRisk;
using ABP.Application.Features.Loans.Queries.GetLoanDetail;
using ABP.Application.Features.Loans.Queries.GetLoans;
using ABP.Domain.Enums;
using ABP.WebApi.Models.Loans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers;

[ApiController]
[Route("api/loan")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class LoansController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] LoanListApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseStatus(request.Status, out var status))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "El estado debe ser activo, completado o todos.");
        }

        var result = await sender.Send(
            new GetLoansQuery(
                new LoanListRequest(
                    request.Page,
                    request.PageSize,
                    request.Identification,
                    status)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await sender.Send(
            new GetLoanDetailQuery(id),
            cancellationToken);

        return detail is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "El préstamo seleccionado no existe.")
            : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        var riskResult = await sender.Send(
            new AssessLoanRiskQuery(request),
            cancellationToken);

        if (riskResult.IsFailure)
        {
            return ToProblem(riskResult.Error);
        }

        if (riskResult.Value.RequiresConfirmation)
        {
            return ToRiskConflict(riskResult.Value);
        }

        var result = await sender.Send(
            new CreateLoanCommand(request),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpPatch("{id:guid}/rate")]
    public async Task<ActionResult> UpdateRate(
        Guid id,
        [FromBody] UpdateLoanRateApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateLoanRateCommand(
                new UpdateLoanRateRequest(
                    id,
                    request.AnnualInterestRate)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private ActionResult ToRiskConflict(
        HighRiskAssessmentDto assessment)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Confirmación de riesgo requerida",
            Detail = LoanErrors.HighRiskConfirmationRequired.Description
        };
        problem.Extensions["riskType"] = assessment.RiskType;
        problem.Extensions["currentDebt"] = assessment.CurrentDebt;
        problem.Extensions["projectedDebt"] = assessment.ProjectedDebt;
        problem.Extensions["averageDebt"] = assessment.AverageDebt;

        return Conflict(problem);
    }

    private ActionResult ToProblem(Error error)
    {
        var (status, title, detail) = error switch
        {
            _ when error == LoanErrors.ClientNotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "El cliente indicado no existe."),
            _ when error == LoanErrors.ClientInactive =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "Solo se puede asignar préstamos a clientes activos."),
            _ when error == LoanErrors.ActiveLoanExists =>
                (StatusCodes.Status409Conflict, "Conflicto", "El cliente ya posee un préstamo activo."),
            _ when error == LoanErrors.NotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "El préstamo seleccionado no existe."),
            _ when error == LoanErrors.Inactive =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "El préstamo debe estar activo para realizar esta operación."),
            _ when error == LoanErrors.HighRiskConfirmationRequired =>
                (StatusCodes.Status409Conflict, "Conflicto", LoanErrors.HighRiskConfirmationRequired.Description),
            _ when error == LoanErrors.NoFuturePendingInstallments =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", LoanErrors.NoFuturePendingInstallments.Description),
            _ when error == LoanErrors.NumberGenerationFailed =>
                (StatusCodes.Status409Conflict, "Conflicto", LoanErrors.NumberGenerationFailed.Description),
            _ when error == LoanErrors.PrincipalAccountNotFound =>
                (StatusCodes.Status409Conflict, "Conflicto", LoanErrors.PrincipalAccountNotFound.Description),
            _ when error == LoanErrors.AdministratorRequired =>
                (StatusCodes.Status403Forbidden, "Acceso denegado", "Se requiere un administrador autenticado."),
            _ when error == LoanErrors.ConcurrencyConflict =>
                (StatusCodes.Status409Conflict, "Conflicto", LoanErrors.ConcurrencyConflict.Description),
            _ =>
                (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };

        return Problem(
            statusCode: status,
            title: title,
            detail: detail);
    }

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
}
