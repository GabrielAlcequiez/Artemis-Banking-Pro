using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.Notifications;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;

public sealed class UpdateCreditLimitCommandHandler(
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IUserRepository users,
    IEmailService emailService,
    IClock clock,
    ILogger<UpdateCreditLimitCommandHandler> logger)
    : IRequestHandler<UpdateCreditLimitCommand, OperationResult>
{
    public async Task<OperationResult> Handle(
        UpdateCreditLimitCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var card = await repository.GetForUpdateAsync(
            request.CreditCardId,
            cancellationToken);

        if (card is null)
        {
            return OperationResult.Failure(CreditCardErrors.NotFound);
        }

        if (card.Status == CreditCardStatus.Cancelled)
        {
            return OperationResult.Failure(CreditCardErrors.Cancelled);
        }

        if (!CreditCardRules.CanChangeLimit(
                card.Status,
                card.Debt,
                request.CreditLimit))
        {
            return OperationResult.Failure(CreditCardErrors.LimitBelowDebt);
        }

        card.Limit = request.CreditLimit;

        var client = await users.GetByIdAsync(
            card.ClientId,
            cancellationToken);
        var recipient = new CardNotificationRecipient(
            card.ClientId,
            client?.Email ?? string.Empty,
            client is null
                ? string.Empty
                : $"{client.Name} {client.LastName}".Trim());
        var cardLastFourDigits = card.CardNumber[^4..];
        var changedAtBankingTime = clock.Now;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await CardNotificationEmails.SendBestEffortAsync(
            emailService,
            logger,
            CardNotificationEmails.LimitChanged(
                recipient,
                cardLastFourDigits,
                request.CreditLimit,
                changedAtBankingTime),
            "modificación de límite",
            card.Id.ToString("N"));

        return OperationResult.Success();
    }
}
