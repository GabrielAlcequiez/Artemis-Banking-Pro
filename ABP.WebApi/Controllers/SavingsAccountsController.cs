using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.Commands.AddBeneficiary;
using ABP.Application.Features.Accounts.Commands.CancelSavingsAccount;
using ABP.Application.Features.Accounts.Commands.CreateSecondaryAccount;
using ABP.Application.Features.Accounts.Commands.Deposit;
using ABP.Application.Features.Accounts.Commands.RemoveBeneficiary;
using ABP.Application.Features.Accounts.Commands.TransferFunds;
using ABP.Application.Features.Accounts.Commands.Withdraw;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Queries.GetAccountTransactions;
using ABP.Application.Features.Accounts.Queries.GetBeneficiaries;
using ABP.Application.Features.Accounts.Queries.GetSavingsAccountDetail;
using ABP.Application.Features.Accounts.Queries.GetSavingsAccounts;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.WebApi.Models.SavingsAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers;

[ApiController]
[Route("api/savings-account")]
[Authorize]
public sealed class SavingsAccountsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(Roles.Administrator))]
    public async Task<ActionResult> GetAll(
        [FromQuery] SavingsAccountListApiRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetSavingsAccountsQuery(
            new PagedRequest(request.Page, request.PageSize),
            request.OwnerIdentification,
            request.Status,
            request.Type);

        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = nameof(Roles.Administrator))]
    public async Task<ActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await sender.Send(
            new GetSavingsAccountDetailQuery(id),
            cancellationToken);

        return detail is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "La cuenta seleccionada no existe.")
            : Ok(detail);
    }

    [HttpGet("{id:guid}/transactions")]
    [Authorize(Roles = nameof(Roles.Administrator))]
    public async Task<ActionResult> GetTransactions(
        Guid id,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAccountTransactionsQuery(id, new PagedRequest(page, pageSize));
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(Roles.Administrator))]
    public async Task<ActionResult> Create(
        [FromBody] CreateSecondaryAccountApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSecondaryAccountCommand(new CreateSecondaryAccountRequest
        {
            OwnerUserId = request.OwnerUserId,
            InitialBalance = request.InitialBalance,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Administrator)
        });

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var detail = await sender.Send(
            new GetSavingsAccountDetailQuery(result.Value),
            cancellationToken);

        if (detail is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error inesperado",
                detail: "La cuenta fue creada, pero no fue posible recuperar su representación.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            SavingsAccountCreatedResponse.From(detail));
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = nameof(Roles.Administrator))]
    public async Task<ActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CancelSavingsAccountCommand(new CancelSavingsAccountRequest
        {
            AccountId = id,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Administrator)
        });

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [HttpPost("transfer")]
    [Authorize(Roles = nameof(Roles.Client))]
    public async Task<ActionResult> Transfer(
        [FromBody] TransferFundsApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransferFundsCommand(new TransferFundsRequest
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountNumber = request.DestinationAccountNumber,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            OperationType = request.OperationType,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Client)
        });

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblem(result.Error);
    }

    [HttpPost("deposit")]
    [Authorize(Roles = nameof(Roles.Cashier))]
    public async Task<ActionResult> Deposit(
        [FromBody] DepositApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DepositCommand(new DepositRequest
        {
            DestinationAccountNumber = request.DestinationAccountNumber,
            Amount = request.Amount,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Cashier)
        });

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblem(result.Error);
    }

    [HttpPost("withdraw")]
    [Authorize(Roles = nameof(Roles.Cashier))]
    public async Task<ActionResult> Withdraw(
        [FromBody] WithdrawApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new WithdrawCommand(new WithdrawalRequest
        {
            SourceAccountNumber = request.SourceAccountNumber,
            Amount = request.Amount,
            ActorUserId = currentUser.UserId ?? string.Empty,
            ActorRole = nameof(Roles.Cashier)
        });

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblem(result.Error);
    }

    [HttpGet("beneficiaries")]
    [Authorize(Roles = nameof(Roles.Client))]
    public async Task<ActionResult> GetBeneficiaries(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBeneficiariesQuery(currentUser.UserId ?? string.Empty),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("beneficiaries")]
    [Authorize(Roles = nameof(Roles.Client))]
    public async Task<ActionResult> AddBeneficiary(
        [FromBody] AddBeneficiaryApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBeneficiaryCommand(new AddBeneficiaryRequest
        {
            OwnerUserId = currentUser.UserId ?? string.Empty,
            BeneficiaryAccountNumber = request.BeneficiaryAccountNumber
        });

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblem(result.Error);
    }

    [HttpDelete("beneficiaries/{id:guid}")]
    [Authorize(Roles = nameof(Roles.Client))]
    public async Task<ActionResult> RemoveBeneficiary(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveBeneficiaryCommand(currentUser.UserId ?? string.Empty, id),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private ActionResult ToProblem(Error error)
    {
        var (status, title, detail) = error switch
        {
            _ when error == AccountErrors.NotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "La cuenta seleccionada no existe."),
            _ when error == AccountErrors.BeneficiaryNotFound =>
                (StatusCodes.Status404NotFound, "No encontrado", "El beneficiario indicado no existe."),
            _ when error == AccountErrors.InvalidAmount =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "El monto debe ser mayor que cero."),
            _ when error == AccountErrors.InsufficientFunds =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "La cuenta no tiene fondos suficientes para esta operación."),
            _ when error == AccountErrors.InactiveAccount =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "La cuenta no se encuentra activa."),
            _ when error == AccountErrors.SameAccount =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "La cuenta de origen y destino deben ser diferentes."),
            _ when error == AccountErrors.CannotAddSelf =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "No puedes agregar tu propia cuenta como beneficiario."),
            _ when error == AccountErrors.CannotCancelPrincipal =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "La cuenta principal no se puede cancelar."),
            _ when error == AccountErrors.AlreadyCancelled =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "La cuenta ya se encuentra cancelada."),
            _ when error == AccountErrors.PrincipalAlreadyExists =>
                (StatusCodes.Status409Conflict, "Conflicto", "El cliente ya tiene una cuenta principal."),
            _ when error == AccountErrors.BeneficiaryAlreadyExists =>
                (StatusCodes.Status409Conflict, "Conflicto", "Esta cuenta ya está registrada como beneficiario."),
            _ when error == AccountErrors.PrincipalNotFound =>
                (StatusCodes.Status409Conflict, "Conflicto", "El saldo no pudo transferirse porque el cliente no tiene cuenta principal."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };

        return Problem(statusCode: status, title: title, detail: detail);
    }
}
