using System.Data;
using System.Globalization;
using System.Net;
using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Exceptions;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;
using ABP.Domain.Rules.Cards;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.HermesPay.Commands.ProcessHermesPayment;

public sealed class ProcessHermesPaymentCommandHandler(
    ICommerceAuthorizationResolverService authorizationResolver,
    ICommerceRepository commerceRepository,
    ICreditCardRepository creditCardRepository,
    ISavingsAccountRepository savingsAccountRepository,
    IAccountBalanceService accountBalanceService,
    IAccountLedger accountLedger,
    IUnitOfWork unitOfWork,
    IFinancialTransaction financialTransaction,
    ICvcService cvcService,
    IClock clock,
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IEmailService emailService,
    ILogger<ProcessHermesPaymentCommandHandler> logger)
    : IRequestHandler<ProcessHermesPaymentCommand, OperationResult<FinancialOperationReceipt>>
{
    public async Task<OperationResult<FinancialOperationReceipt>> Handle(
        ProcessHermesPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        HermesPaymentNotification? notification = null;

        try
        {
            var result = await financialTransaction.ExecuteAsync(
                IsolationLevel.Serializable,
                transactionCancellationToken => ProcessInsideTransactionAsync(
                    request,
                    scheduledNotification => notification = scheduledNotification,
                    transactionCancellationToken),
                cancellationToken);

            if (result.IsSuccess && notification is not null)
            {
                await SendNotificationsBestEffortAsync(
                    notification,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception exception)
            when (exception is PersistenceConflictException or
                  FinancialConcurrencyException)
        {
            var concurrentConsumption = await creditCardRepository
                .GetConsumptionByOperationIdAsync(
                    request.OperationId,
                    cancellationToken);
            if (concurrentConsumption is null)
            {
                throw;
            }

            return await ResolveConcurrentReplayAsync(
                concurrentConsumption,
                request,
                cancellationToken);
        }
    }

    private async Task<OperationResult<FinancialOperationReceipt>> ProcessInsideTransactionAsync(
        ProcessHermesPaymentRequest request,
        Action<HermesPaymentNotification> scheduleNotification,
        CancellationToken cancellationToken)
    {
        var contextResult = await ResolveCommerceContextAsync(
            request.RequestedCommerceId,
            cancellationToken);
        if (contextResult.IsFailure)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                contextResult.Error);
        }

        var commerceContext = contextResult.Value;
        var previousConsumption = await creditCardRepository
            .GetConsumptionByOperationIdAsync(
                request.OperationId,
                cancellationToken);
        var card = await creditCardRepository.GetByCardNumberForUpdateAsync(
            request.CardNumber,
            cancellationToken);

        if (previousConsumption is not null)
        {
            return ResolveReplay(
                previousConsumption,
                request,
                commerceContext,
                card);
        }

        var principalAccount = await savingsAccountRepository
            .GetPrincipalAccountAsync(
                commerceContext.Commerce.AssociatedUser!.Id,
                cancellationToken);
        if (principalAccount is null ||
            principalAccount.Status != SavingsAccountStatus.Active)
        {
            if (card is not null)
            {
                await RecordRejectedAsync(
                    card,
                    commerceContext,
                    principalAccount,
                    request,
                    HermesPayErrors.PrimaryAccountRequired,
                    cancellationToken);
            }

            return OperationResult<FinancialOperationReceipt>.Failure(
                HermesPayErrors.PrimaryAccountRequired);
        }

        var cardError = ValidateCard(card, request, clock.Today, cvcService);
        if (cardError is not null)
        {
            if (card is not null)
            {
                await RecordRejectedAsync(
                    card,
                    commerceContext,
                    principalAccount,
                    request,
                    cardError,
                    cancellationToken);
            }

            return OperationResult<FinancialOperationReceipt>.Failure(cardError);
        }

        var creditResult = await accountBalanceService.CreditAsync(
            principalAccount.Id,
            request.TransactionAmount,
            cancellationToken);
        if (creditResult.IsFailure)
        {
            await RecordRejectedAsync(
                card!,
                commerceContext,
                principalAccount,
                request,
                HermesPayErrors.PrimaryAccountRequired,
                cancellationToken);

            return OperationResult<FinancialOperationReceipt>.Failure(
                HermesPayErrors.PrimaryAccountRequired);
        }

        var processedAtUtc = clock.UtcNow;
        card!.Debt += request.TransactionAmount;
        await creditCardRepository.AddConsumptionAsync(
            CreateConsumption(
                card,
                commerceContext,
                principalAccount,
                request,
                ConsumptionStatus.Approved,
                processedAtUtc),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await accountLedger.RecordApprovedAsync(
            request.OperationId,
            principalAccount.Id,
            request.TransactionAmount,
            TransactionDirection.Credit,
            FinancialOperationType.HermesPayment,
            LastFour(card.CardNumber),
            principalAccount.AccountNumber,
            currentUser.UserId,
            CurrentRole(),
            cancellationToken);

        scheduleNotification(
            new HermesPaymentNotification(
                request.OperationId,
                card.ClientId,
                LastFour(card.CardNumber),
                commerceContext.Commerce.Name,
                commerceContext.Commerce.Email,
                request.TransactionAmount,
                clock.Now));

        return OperationResult<FinancialOperationReceipt>.Success(
            new FinancialOperationReceipt(
                request.OperationId,
                request.TransactionAmount,
                processedAtUtc));
    }

    private async Task<OperationResult<CommerceContext>> ResolveCommerceContextAsync(
        Guid requestedCommerceId,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizationResolver
            .ResolveAuthorizedCommerceIdAsync(
                requestedCommerceId,
                cancellationToken);
        if (authorization.IsFailure)
        {
            return OperationResult<CommerceContext>.Failure(authorization.Error);
        }

        var commerceId = authorization.Value;
        var commerce = await commerceRepository.GetDetailsAsync(
            commerceId,
            cancellationToken);
        if (commerce is null)
        {
            return OperationResult<CommerceContext>.Failure(
                HermesPayErrors.CommerceNotFound);
        }

        if (commerce.Status != CommerceStatus.Active)
        {
            return OperationResult<CommerceContext>.Failure(
                HermesPayErrors.CommerceInactive);
        }

        if (commerce.AssociatedUser is null)
        {
            return OperationResult<CommerceContext>.Failure(
                HermesPayErrors.AssociatedCommerceUserRequired);
        }

        if (!commerce.AssociatedUser.IsActive)
        {
            return OperationResult<CommerceContext>.Failure(
                HermesPayErrors.AssociatedCommerceUserInactive);
        }

        return OperationResult<CommerceContext>.Success(
            new CommerceContext(commerceId, commerce));
    }

    private async Task<OperationResult<FinancialOperationReceipt>> ResolveConcurrentReplayAsync(
        CardConsumption consumption,
        ProcessHermesPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var contextResult = await ResolveCommerceContextAsync(
            request.RequestedCommerceId,
            cancellationToken);
        if (contextResult.IsFailure)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                contextResult.Error);
        }

        var card = await creditCardRepository.GetByCardNumberAsync(
            request.CardNumber,
            cancellationToken);

        return ResolveReplay(
            consumption,
            request,
            contextResult.Value,
            card);
    }

    private async Task RecordRejectedAsync(
        CreditCard card,
        CommerceContext commerceContext,
        Domain.Entities.Accounts.SavingsAccount? principalAccount,
        ProcessHermesPaymentRequest request,
        Error error,
        CancellationToken cancellationToken)
    {
        await creditCardRepository.AddConsumptionAsync(
            CreateConsumption(
                card,
                commerceContext,
                principalAccount,
                request,
                ConsumptionStatus.Rejected,
                clock.UtcNow,
                error),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private CardConsumption CreateConsumption(
        CreditCard card,
        CommerceContext commerceContext,
        Domain.Entities.Accounts.SavingsAccount? principalAccount,
        ProcessHermesPaymentRequest request,
        ConsumptionStatus status,
        DateTimeOffset occurredAtUtc,
        Error? error = null) =>
        new()
        {
            CreditCardId = card.Id,
            CommerceId = commerceContext.CommerceId,
            TargetAccountId = principalAccount?.Id,
            CommerceName = commerceContext.Commerce.Name,
            RequestedAmount = request.TransactionAmount,
            Amount = request.TransactionAmount,
            Status = status,
            OccurredAtUtc = occurredAtUtc,
            OperationId = request.OperationId,
            ActorUserId = currentUser.UserId,
            FailureCode = error?.Code,
            FailureDescription = error?.Description
        };

    private OperationResult<FinancialOperationReceipt> ResolveReplay(
        CardConsumption consumption,
        ProcessHermesPaymentRequest request,
        CommerceContext commerceContext,
        CreditCard? card)
    {
        if (card is null ||
            consumption.ActorUserId != currentUser.UserId ||
            consumption.CreditCardId != card.Id ||
            consumption.CommerceId != commerceContext.CommerceId ||
            consumption.RequestedAmount != request.TransactionAmount)
        {
            return OperationResult<FinancialOperationReceipt>.Failure(
                HermesPayErrors.OperationIdConflict);
        }

        return consumption.Status == ConsumptionStatus.Approved
            ? OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(
                    consumption.OperationId,
                    consumption.RequestedAmount.Value,
                    consumption.OccurredAtUtc))
            : OperationResult<FinancialOperationReceipt>.Failure(
                HermesPayErrors.ResolvePersisted(
                    consumption.FailureCode,
                    consumption.FailureDescription));
    }

    private static Error? ValidateCard(
        CreditCard? card,
        ProcessHermesPaymentRequest request,
        DateOnly bankingDate,
        ICvcService cvcService)
    {
        if (card is null)
        {
            return HermesPayErrors.CardNotFound;
        }

        if (card.Status != CreditCardStatus.Active)
        {
            return HermesPayErrors.CardInactive;
        }

        if (CreditCardRules.IsExpired(card.ExpirationDate, bankingDate))
        {
            return HermesPayErrors.CardExpired;
        }

        if (card.ExpirationDate.Month != request.ExpirationMonth ||
            card.ExpirationDate.Year != request.ExpirationYear ||
            !cvcService.Verify(request.Cvc, card.CvcHash))
        {
            return HermesPayErrors.CardDataMismatch;
        }

        return request.TransactionAmount > card.AvailableCredit
            ? HermesPayErrors.InsufficientCredit
            : null;
    }

    private async Task SendNotificationsBestEffortAsync(
        HermesPaymentNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await userRepository.GetByIdAsync(
                notification.ClientId,
                cancellationToken);
            if (client is null || string.IsNullOrWhiteSpace(client.Email))
            {
                logger.LogWarning(
                    "No se pudo preparar la notificación Hermes al cliente para la operación {OperationId}: cliente o correo no disponible.",
                    notification.OperationId);
            }
            else
            {
                await SendEmailBestEffortAsync(
                    new EmailRequestDto
                    {
                        ToEmail = client.Email,
                        RecipientName = FullName(client.Name, client.LastName),
                        Subject = $"Consumo realizado con la tarjeta {notification.CardLastFourDigits}",
                        Body = ClientEmailBody(notification, FullName(client.Name, client.LastName))
                    },
                    "cliente",
                    notification.OperationId);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "No se pudo consultar el destinatario cliente de la notificación Hermes para la operación {OperationId}.",
                notification.OperationId);
        }

        await SendEmailBestEffortAsync(
            new EmailRequestDto
            {
                ToEmail = notification.CommerceEmail,
                RecipientName = notification.CommerceName,
                Subject = $"Pago recibido a través de tarjeta {notification.CardLastFourDigits}",
                Body = CommerceEmailBody(notification)
            },
            "comercio",
            notification.OperationId);
    }

    private async Task SendEmailBestEffortAsync(
        EmailRequestDto email,
        string recipientType,
        Guid operationId)
    {
        try
        {
            await emailService.SendAsync(email);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "No se pudo enviar la notificación Hermes al {RecipientType} para la operación {OperationId}. El pago permanece aprobado.",
                recipientType,
                operationId);
        }
    }

    private static string ClientEmailBody(
        HermesPaymentNotification notification,
        string clientName) =>
        $"""
        <p>Hola {Encode(clientName)},</p>
        <p>Se ha realizado un consumo con su tarjeta terminada en {notification.CardLastFourDigits}.</p>
        <p>Comercio: {Encode(notification.CommerceName)}</p>
        <p>Monto: RD${FormatAmount(notification.Amount)}</p>
        <p>Fecha y hora: {FormatDateTime(notification.OccurredAtBankingTime)}</p>
        <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
        """;

    private static string CommerceEmailBody(HermesPaymentNotification notification) =>
        $"""
        <p>Hola {Encode(notification.CommerceName)},</p>
        <p>Ha recibido un nuevo pago mediante Hermes Pay.</p>
        <p>Tarjeta terminada en: {notification.CardLastFourDigits}</p>
        <p>Monto recibido: RD${FormatAmount(notification.Amount)}</p>
        <p>Fecha y hora: {FormatDateTime(notification.OccurredAtBankingTime)}</p>
        <p>Este mensaje sirve como constancia del pago recibido.</p>
        """;

    private static string FullName(string name, string lastName) =>
        $"{name} {lastName}".Trim();

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N2", CultureInfo.GetCultureInfo("es-DO"));

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("dd/MM/yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);

    private string CurrentRole() =>
        currentUser.IsInRole(Roles.Administrator.ToString())
            ? Roles.Administrator.ToString()
            : Roles.Commerce.ToString();

    private static string LastFour(string cardNumber) => cardNumber[^4..];

    private sealed record CommerceContext(
        Guid CommerceId,
        CommerceDetailReadModel Commerce);

    private sealed record HermesPaymentNotification(
        Guid OperationId,
        string ClientId,
        string CardLastFourDigits,
        string CommerceName,
        string CommerceEmail,
        decimal Amount,
        DateTimeOffset OccurredAtBankingTime);
}
