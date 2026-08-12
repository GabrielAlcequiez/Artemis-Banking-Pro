namespace ABP.Application.Common.Interfaces.Services;

public interface ICommerceUserInactivationService
{
    Task InactivateAssociatedUsersAndCommitAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default);
}
