using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.Rules.Cards;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardCommandHandler(
    ICvcService cvcService,
    ICardNumberGeneratorService numberGeneratorService,
    ICreditCardRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateCreditCardCommand, OperationResult<Guid>>
{
    private const int MaxCardNumberGenerationAttempts = 10;

    public async Task<OperationResult<Guid>> Handle(
        CreateCreditCardCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        var assignedByUserId =
            currentUser.IsAuthenticated &&
            currentUser.IsInRole(Roles.Administrator.ToString())
                ? currentUser.UserId
                : null;

        if (string.IsNullOrWhiteSpace(assignedByUserId))
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.AdministratorRequired);
        }

        var clientExists = await repository.ClientExistsAsync(
            request.ClientId,
            cancellationToken);

        if (!clientExists)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.ClientNotFound);
        }

        var isActiveClient = await repository.IsActiveClientAsync(
            request.ClientId,
            cancellationToken);

        if (!isActiveClient)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.ClientInactive);
        }

        var cardNumber = await GenerateUniqueCardNumberAsync(
            cancellationToken);

        if (cardNumber is null)
        {
            return OperationResult<Guid>.Failure(
                CreditCardErrors.NumberGenerationFailed);
        }

        var cvc = cvcService.Generate();
        var card = new CreditCard
        {
            ClientId = request.ClientId,
            CardNumber = cardNumber,
            CvcHash = cvcService.Hash(cvc),
            Limit = request.CreditLimit,
            Debt = 0m,
            ExpirationDate = CreditCardRules.CalculateExpirationDate(clock.Today),
            Status = CreditCardStatus.Active,
            AssignedByUserId = assignedByUserId
        };

        var createdCard = await repository.AddAsync(
            card,
            cancellationToken);

        // TODO(P1 Outbox): enqueue the assignment email in this transaction so it is
        // dispatched only after commit. Never include the full PAN, CVC, or CVC hash.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(createdCard.Id);
    }

    private async Task<string?> GenerateUniqueCardNumberAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0;
             attempt < MaxCardNumberGenerationAttempts;
             attempt++)
        {
            var candidate = numberGeneratorService.Generate();
            var alreadyExists = await repository.CardNumberExistsAsync(
                candidate,
                cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        return null;
    }
}
