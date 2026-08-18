using ABP.Application.Common;
using ABP.Application.Features.Commerce;
using ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;
using ABP.Application.Features.Commerce.Commands.CreateCommerce;
using ABP.Application.Features.Commerce.Commands.UpdateCommerce;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Queries.GetCommerceDetail;
using ABP.Application.Features.Commerce.Queries.GetCommerces;
using ABP.Domain.Enums;
using ABP.WebApi.Models.Commerce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers;

[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class CommerceController(ISender sender) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] CommerceListApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseStatus(request.Status, out var status))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "El estado debe ser activo, inactivo o todos.");
        }

        var result = await sender.Send(
            new GetCommercesQuery(
                new CommerceListRequest(
                    request.Page,
                    request.PageSize,
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
            new GetCommerceDetailQuery(id),
            cancellationToken);

        return detail is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: CommerceErrors.NotFound.Description)
            : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateCommerceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCommerceCommand(request),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var detail = await sender.Send(
            new GetCommerceDetailQuery(result.Value),
            cancellationToken);

        if (detail is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error inesperado",
                detail: "El comercio fue creado, pero no fue posible recuperar su representación.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            CommerceCreatedResponse.From(detail));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateCommerceApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCommerceCommand(
                new UpdateCommerceRequest(
                    id,
                    request.Name,
                    request.Description,
                    request.Email,
                    request.PhoneNumber,
                    request.Rnc)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeCommerceStatusApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Status.HasValue)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "El campo status es requerido.");
        }

        var result = await sender.Send(
            new ChangeCommerceStatusCommand(
                new ChangeCommerceStatusRequest(id, request.Status.Value)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private ActionResult ToProblem(Error error)
    {
        var (status, title, detail) = error switch
        {
            _ when error == CommerceErrors.NotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", error.Description),
            _ when error == CommerceErrors.DuplicateEmail =>
                (StatusCodes.Status409Conflict, "Conflicto", error.Description),
            _ when error == CommerceErrors.DuplicateRnc =>
                (StatusCodes.Status409Conflict, "Conflicto", error.Description),
            _ when error == CommerceErrors.AdministratorRequired =>
                (StatusCodes.Status403Forbidden, "Acceso denegado", error.Description),
            _ when error == CommerceErrors.ConcurrencyConflict =>
                (StatusCodes.Status409Conflict, "Conflicto", error.Description),
            _ =>
                (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };

        return Problem(statusCode: status, title: title, detail: detail);
    }

    private static bool TryParseStatus(
        string? value,
        out CommerceStatusFilter? status)
    {
        status = value?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "activo" => CommerceStatusFilter.Active,
            "inactivo" => CommerceStatusFilter.Inactive,
            "todos" => CommerceStatusFilter.All,
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) || status.HasValue;
    }
}
