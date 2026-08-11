using ABP.Application.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;

public sealed class UpdateCreditLimitCommandHandler(
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork)
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

        // TODO(P1 Outbox): enqueue the limit-change email in this transaction so it is
        // dispatched only after commit. Include only the last four digits, never the PAN.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
