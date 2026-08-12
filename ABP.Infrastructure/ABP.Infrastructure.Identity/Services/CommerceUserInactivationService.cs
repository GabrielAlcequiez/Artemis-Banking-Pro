using System.Data;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ABP.Infrastructure.Identity.Services;

public sealed class CommerceUserInactivationService(
    IUnitOfWork unitOfWork,
    IdentityContext identityContext,
    AppDbContext appContext) : ICommerceUserInactivationService
{
    public async Task InactivateAssociatedUsersAndCommitAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await appContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        identityContext.Database.SetDbConnection(
            appContext.Database.GetDbConnection(),
            contextOwnsConnection: false);
        await identityContext.Database.UseTransactionAsync(
            transaction.GetDbTransaction(),
            cancellationToken);

        var domainUsers = await appContext.Users
            .Where(user =>
                user.CommerceId == commerceId &&
                user.Role == Roles.Commerce)
            .ToListAsync(cancellationToken);

        if (domainUsers.Count > 0)
        {
            var userIds = domainUsers
                .Select(user => user.Id)
                .ToArray();
            var identityUsers = await identityContext.Users
                .Where(user => userIds.Contains(user.Id))
                .ToListAsync(cancellationToken);

            foreach (var domainUser in domainUsers)
            {
                domainUser.IsActive = false;
            }

            foreach (var identityUser in identityUsers)
            {
                identityUser.IsActive = false;
                identityUser.EmailConfirmed = false;
                identityUser.SecurityStamp = Guid.NewGuid().ToString("N");
            }

            await identityContext.SaveChangesAsync(cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
