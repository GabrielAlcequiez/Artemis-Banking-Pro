using System.Globalization;
using ABP.Application.Common;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;
using ABP.Domain.Enums;
using ABP.WebApi.Models.HermesPay;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers;

[ApiController]
[Route("pay")]
[Authorize(Roles = nameof(Roles.Administrator) + "," + nameof(Roles.Commerce))]
public sealed class PayController(ISender sender) : ControllerBase
{
    [HttpGet("get-transactions/{commerceId:guid}")]
    [ProducesResponseType(typeof(HermesTransactionsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetTransactions(
        Guid commerceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetHermesTransactionsQuery(commerceId, page, pageSize),
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblem(result.Error);
    }

    [HttpPost("process-payment/{commerceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ProcessPayment(
        Guid commerceId,
        [FromHeader(Name = "Idempotency-Key")] Guid operationId,
        [FromBody] ProcessHermesPaymentApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseExpiration(
                request.MonthExpirationCard,
                request.YearExpirationCard,
                out var expirationMonth,
                out var expirationYear))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "La expiración de la tarjeta debe enviarse con mes MM y año YYYY válidos.");
        }

        var result = await sender.Send(
            new ProcessHermesPaymentCommand(
                new ProcessHermesPaymentRequest(
                    commerceId,
                    request.CardNumber,
                    expirationMonth,
                    expirationYear,
                    request.Cvc,
                    request.TransactionAmount,
                    operationId)),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private ActionResult ToProblem(Error error)
    {
        var (status, title, detail) = error switch
        {
            _ when error == HermesPayErrors.AuthenticationRequired =>
                (StatusCodes.Status401Unauthorized, "No autorizado", error.Description),
            _ when error == HermesPayErrors.RoleNotAllowed ||
                error == HermesPayErrors.CommerceUserInactive ||
                error == HermesPayErrors.CommerceAssociationRequired =>
                (StatusCodes.Status403Forbidden, "Acceso denegado", error.Description),
            _ when error == HermesPayErrors.CommerceNotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", error.Description),
            _ when error == HermesPayErrors.OperationIdConflict =>
                (StatusCodes.Status409Conflict, "Conflicto de operación", error.Description),
            _ when error == HermesPayErrors.CommerceInactive ||
                error == HermesPayErrors.AssociatedCommerceUserRequired ||
                error == HermesPayErrors.AssociatedCommerceUserInactive ||
                error == HermesPayErrors.PrimaryAccountRequired ||
                error == HermesPayErrors.CardNotFound ||
                error == HermesPayErrors.CardInactive ||
                error == HermesPayErrors.CardExpired ||
                error == HermesPayErrors.CardDataMismatch ||
                error == HermesPayErrors.InsufficientCredit =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", error.Description),
            _ =>
                (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };

        return Problem(statusCode: status, title: title, detail: detail);
    }

    private static bool TryParseExpiration(
        string? monthValue,
        string? yearValue,
        out int month,
        out int year)
    {
        month = 0;
        year = 0;

        return monthValue is not null &&
            yearValue is not null &&
            monthValue.Length == 2 &&
            yearValue.Length == 4 &&
            int.TryParse(
                monthValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out month) &&
            int.TryParse(
                yearValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out year) &&
            month is >= 1 and <= 12;
    }
}
