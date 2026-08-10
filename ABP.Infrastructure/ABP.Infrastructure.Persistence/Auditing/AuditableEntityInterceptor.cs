using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ABP.Infrastructure.Persistence.Auditing;

public sealed class AuditableEntityInterceptor(
    IClock clock,
    ICurrentUserService currentUser) : SaveChangesInterceptor
{
    private const string CreatedAtUtcProperty = nameof(AuditableEntity<Guid>.CreatedAtUtc);
    private const string CreatedByUserIdProperty = nameof(AuditableEntity<Guid>.CreatedByUserId);
    private const string LastModifiedAtUtcProperty = nameof(AuditableEntity<Guid>.LastModifiedAtUtc);
    private const string LastModifiedByUserIdProperty = nameof(AuditableEntity<Guid>.LastModifiedByUserId);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditValues(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditValues(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditValues(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var utcNow = clock.UtcNow;
        var actorUserId = GetAuthenticatedUserId();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreationAudit(entry, utcNow, actorUserId);
                    break;
                case EntityState.Modified:
                    ApplyModificationAudit(entry, utcNow, actorUserId);
                    break;
            }
        }
    }

    private string? GetAuthenticatedUserId()
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        var userId = currentUser.UserId;

        return string.IsNullOrWhiteSpace(userId)
            ? null
            : userId;
    }

    private static void ApplyCreationAudit(
        EntityEntry entry,
        DateTimeOffset utcNow,
        string? actorUserId)
    {
        if (entry.Metadata.FindProperty(CreatedAtUtcProperty) is not null)
        {
            var createdAt = entry.Property(CreatedAtUtcProperty);

            if (Equals(createdAt.CurrentValue, default(DateTimeOffset)))
            {
                createdAt.CurrentValue = utcNow;
            }
        }

        if (entry.Metadata.FindProperty(CreatedByUserIdProperty) is not null)
        {
            var createdBy = entry.Property(CreatedByUserIdProperty);

            if (string.IsNullOrWhiteSpace(createdBy.CurrentValue as string))
            {
                createdBy.CurrentValue = actorUserId;
            }
        }
    }

    private static void ApplyModificationAudit(
        EntityEntry entry,
        DateTimeOffset utcNow,
        string? actorUserId)
    {
        if (entry.Metadata.FindProperty(LastModifiedAtUtcProperty) is not null)
        {
            entry.Property(LastModifiedAtUtcProperty).CurrentValue = utcNow;
        }

        if (entry.Metadata.FindProperty(LastModifiedByUserIdProperty) is not null)
        {
            entry.Property(LastModifiedByUserIdProperty).CurrentValue = actorUserId;
        }
    }
}
