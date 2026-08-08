using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ABP.Infrastructure.Persistence.Auditing;

public sealed class AuditTimestampInterceptor(IClock clock) : SaveChangesInterceptor
{
    private const string CreatedAtUtcProperty = nameof(AuditableEntity<Guid>.CreatedAtUtc);
    private const string LastModifiedAtUtcProperty = nameof(AuditableEntity<Guid>.LastModifiedAtUtc);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTimestamps(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var utcNow = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added &&
                entry.Metadata.FindProperty(CreatedAtUtcProperty) is not null)
            {
                var createdAt = entry.Property(CreatedAtUtcProperty);

                if (Equals(createdAt.CurrentValue, default(DateTimeOffset)))
                {
                    createdAt.CurrentValue = utcNow;
                }
            }

            if (entry.State == EntityState.Modified &&
                entry.Metadata.FindProperty(LastModifiedAtUtcProperty) is not null)
            {
                entry.Property(LastModifiedAtUtcProperty).CurrentValue = utcNow;
            }
        }
    }
}
