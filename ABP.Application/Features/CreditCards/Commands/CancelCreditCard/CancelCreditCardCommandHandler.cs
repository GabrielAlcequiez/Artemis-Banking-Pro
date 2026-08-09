using ABP.Application.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.CancelCreditCard;

public sealed class CancelCreditCardCommandHandler(
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelCreditCardCommand, OperationResult>
{
    public async Task<OperationResult> Handle(
        CancelCreditCardCommand command,
        CancellationToken cancellationToken)
    {
        var card = await repository.GetForUpdateAsync(
            command.Request.CreditCardId,
            cancellationToken);

        if (card is null)
        {
            return OperationResult.Failure(CreditCardErrors.NotFound);
        }

        if (card.Status == CreditCardStatus.Cancelled)
        {
            return OperationResult.Failure(CreditCardErrors.Cancelled);
        }

        if (!CreditCardRules.CanCancel(card.Status, card.Debt))
        {
            return OperationResult.Failure(CreditCardErrors.OutstandingDebt);
        }

        card.Status = CreditCardStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
