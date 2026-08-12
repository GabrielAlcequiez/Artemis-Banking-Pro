using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using MediatR;

namespace ABP.Application.Features.Commerce.Commands.UpdateCommerce;

public sealed class UpdateCommerceCommandHandler(
    ICommerceRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateCommerceCommand, OperationResult>
{
    public async Task<OperationResult> Handle(
        UpdateCommerceCommand command,
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

        var data = CommerceDataNormalizer.Normalize(
            request.Name,
            request.Description,
            request.Email,
            request.PhoneNumber,
            request.Rnc);

        if (await repository.EmailExistsAsync(
                data.Email,
                request.CommerceId,
                cancellationToken))
        {
            return OperationResult.Failure(CommerceErrors.DuplicateEmail);
        }

        if (await repository.RncExistsAsync(
                data.Rnc,
                request.CommerceId,
                cancellationToken))
        {
            return OperationResult.Failure(CommerceErrors.DuplicateRnc);
        }

        commerce.Name = data.Name;
        commerce.Description = data.Description;
        commerce.Email = data.Email;
        commerce.PhoneNumber = data.PhoneNumber;
        commerce.Rnc = data.Rnc;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }

    private bool HasAuthenticatedAdministrator() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.IsInRole(Roles.Administrator.ToString());
}
