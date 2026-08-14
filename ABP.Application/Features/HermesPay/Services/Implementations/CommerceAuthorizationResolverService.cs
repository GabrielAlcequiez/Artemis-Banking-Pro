using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;

namespace ABP.Application.Features.HermesPay.Services.Implementations;

public sealed class CommerceAuthorizationResolverService(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    ICommerceRepository commerceRepository)
    : ICommerceAuthorizationResolverService
{
    public async Task<OperationResult<Guid>> ResolveAuthorizedCommerceIdAsync(
        Guid requestedCommerceId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.AuthenticationRequired);
        }

        Guid commerceId;
        if (currentUser.IsInRole(Roles.Administrator.ToString()))
        {
            commerceId = requestedCommerceId;
        }
        else if (currentUser.IsInRole(Roles.Commerce.ToString()))
        {
            var persistedUser = await userRepository.GetByIdAsync(
                currentUser.UserId,
                cancellationToken);

            if (persistedUser is null ||
                persistedUser.Role != Roles.Commerce ||
                !persistedUser.IsActive)
            {
                return OperationResult<Guid>.Failure(
                    HermesPayErrors.CommerceUserInactive);
            }

            if (!persistedUser.CommerceId.HasValue)
            {
                return OperationResult<Guid>.Failure(
                    HermesPayErrors.CommerceAssociationRequired);
            }

            commerceId = persistedUser.CommerceId.Value;
        }
        else
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.RoleNotAllowed);
        }

        var commerce = await commerceRepository.GetDetailsAsync(
            commerceId,
            cancellationToken);

        return ValidateCommerce(commerce, commerceId);
    }

    private static OperationResult<Guid> ValidateCommerce(
        CommerceDetailReadModel? commerce,
        Guid commerceId)
    {
        if (commerce is null)
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.CommerceNotFound);
        }

        if (commerce.Status != CommerceStatus.Active)
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.CommerceInactive);
        }

        if (commerce.AssociatedUser is null)
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.AssociatedCommerceUserRequired);
        }

        if (!commerce.AssociatedUser.IsActive)
        {
            return OperationResult<Guid>.Failure(
                HermesPayErrors.AssociatedCommerceUserInactive);
        }

        return OperationResult<Guid>.Success(commerceId);
    }
}
