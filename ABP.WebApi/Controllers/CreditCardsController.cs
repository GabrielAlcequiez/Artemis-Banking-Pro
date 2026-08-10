using ABP.Application.Common;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.Commands.CancelCreditCard;
using ABP.Application.Features.CreditCards.Commands.CreateCreditCard;
using ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Queries.GetCreditCardDetail;
using ABP.Application.Features.CreditCards.Queries.GetCreditCards;
using ABP.Domain.Enums;
using ABP.WebApi.Models.CreditCards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers;

[ApiController]
[Route("api/credit-card")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class CreditCardsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] CreditCardListApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseStatus(request.Status, out var status))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "El estado debe ser activa, cancelada o todas.");
        }

        var query = new GetCreditCardsQuery(
            new CreditCardListRequest(
                request.Page,
                request.PageSize,
                request.Identification,
                status));
        var result = await sender.Send(query, cancellationToken);

        return Ok(result.Page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await sender.Send(
            new GetCreditCardDetailQuery(id),
            cancellationToken);

        return detail is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "La tarjeta seleccionada no existe.")
            : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCreditCardCommand(request),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var detail = await sender.Send(
            new GetCreditCardDetailQuery(result.Value),
            cancellationToken);

        if (detail is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error inesperado",
                detail: "La tarjeta fue creada, pero no fue posible recuperar su representación.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            CreditCardCreatedResponse.From(detail));
    }

    [HttpPatch("{id:guid}/limit")]
    public async Task<ActionResult> UpdateLimit(
        Guid id,
        [FromBody] UpdateCreditLimitApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCreditLimitCommand(
                new UpdateCreditLimitRequest(id, request.CreditLimit)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelCreditCardCommand(new CancelCreditCardRequest(id)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private ActionResult ToProblem(Error error)
    {
        var (status, title, detail) = error switch
        {
            _ when error == CreditCardErrors.ClientNotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "El cliente indicado no existe."),
            _ when error == CreditCardErrors.ClientInactive =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "Solo se puede asignar tarjetas de crédito a clientes activos."),
            _ when error == CreditCardErrors.ClientNotEligible =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "Solo se puede asignar tarjetas de crédito a clientes activos."),
            _ when error == CreditCardErrors.NotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "La tarjeta seleccionada no existe."),
            _ when error == CreditCardErrors.Cancelled =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "No se puede modificar una tarjeta cancelada."),
            _ when error == CreditCardErrors.LimitBelowDebt =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente."),
            _ when error == CreditCardErrors.OutstandingDebt =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente."),
            _ when error == CreditCardErrors.NumberGenerationFailed =>
                (StatusCodes.Status409Conflict, "Conflicto", "No fue posible generar un número de tarjeta único."),
            _ when error == CreditCardErrors.AdministratorRequired =>
                (StatusCodes.Status403Forbidden, "Acceso denegado", "Se requiere un administrador autenticado."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };

        return Problem(statusCode: status, title: title, detail: detail);
    }

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
}
