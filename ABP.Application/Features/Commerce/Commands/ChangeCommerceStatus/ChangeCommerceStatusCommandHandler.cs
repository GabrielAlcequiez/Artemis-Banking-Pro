using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using MediatR;

namespace ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;

public sealed class ChangeCommerceStatusCommandHandler(
    ICommerceRepository repository,
    IUnitOfWork unitOfWork,
    ICommerceUserInactivationService userInactivationService,
    ICurrentUserService currentUser)
    : IRequestHandler<ChangeCommerceStatusCommand, OperationResult>
{
    public async Task<OperationResult> Handle(
        ChangeCommerceStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!HasAuthenticatedAdministrator())
        {
            return OperationResult.Failure(CommerceErrors.AdministratorRequired);
        }

        var request = command.Request;
        var commerce = await repository.GetForUpdateAsync(
            request.CommerceId,
            cancellationToken);

        if (commerce is null)
        {
            return OperationResult.Failure(CommerceErrors.NotFound);
        }

        if (request.IsActive)
        {
            if (commerce.Status == CommerceStatus.Active)
            {
                return OperationResult.Success();
            }

            commerce.Status = CommerceStatus.Active;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }

        commerce.Status = CommerceStatus.Inactive;
        await userInactivationService.InactivateAssociatedUsersAndCommitAsync(
            request.CommerceId,
            cancellationToken);

        return OperationResult.Success();
    }

    private bool HasAuthenticatedAdministrator() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.IsInRole(Roles.Administrator.ToString());
}
