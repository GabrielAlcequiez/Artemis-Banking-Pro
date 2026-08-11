using ABP.Domain.Common;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories;

public sealed class CommerceRepository(AppDbContext context)
    : GenericRepository<Commerce, Guid>(context), ICommerceRepository
{
    public Task<bool> EmailExistsAsync(
        string email,
        Guid? excludingCommerceId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        return Entities
            .AsNoTracking()
            .AnyAsync(
                commerce =>
                    commerce.Email == normalizedEmail &&
                    (!excludingCommerceId.HasValue || commerce.Id != excludingCommerceId.Value),
                cancellationToken);
    }

    public Task<bool> RncExistsAsync(
        string rnc,
        Guid? excludingCommerceId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRnc = rnc.Trim();

        return Entities
            .AsNoTracking()
            .AnyAsync(
                commerce =>
                    commerce.Rnc == normalizedRnc &&
                    (!excludingCommerceId.HasValue || commerce.Id != excludingCommerceId.Value),
                cancellationToken);
    }

    public async Task<PagedResult<CommerceSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        CommerceStatusFilter? status = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 20);

        var query = Entities.AsNoTracking();

        switch (status)
        {
            case null:
            case CommerceStatusFilter.Active:
                query = query.Where(c => c.Status == CommerceStatus.Active);
                break;
            case CommerceStatusFilter.Inactive:
                query = query.Where(c => c.Status == CommerceStatus.Inactive);
                break;
            case CommerceStatusFilter.All:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown commerce status filter.");
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var orderedQuery = query.OrderByDescending(c => c.CreatedAtUtc);

        var skip = (int)Math.Min((long)(normalizedPage - 1) * normalizedPageSize, int.MaxValue);

        var data = await orderedQuery
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(c => new CommerceSummaryReadModel(
                c.Id,
                c.Name,
                c.Description,
                c.Email,
                c.PhoneNumber,
                c.Rnc,
                c.Status,
                _context.Users.Any(u => u.CommerceId == c.Id && u.Role == Roles.Commerce),
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommerceSummaryReadModel>(data, normalizedPage, normalizedPageSize, totalRecords);
    }

    public async Task<CommerceDetailReadModel?> GetDetailsAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        var commerce = await Entities
            .AsNoTracking()
            .Where(entity => entity.Id == commerceId)
            .Select(entity => new CommerceDetailReadModel(
                entity.Id,
                entity.Name,
                entity.Description,
                entity.Email,
                entity.PhoneNumber,
                entity.Rnc,
                entity.Status,
                entity.CreatedAtUtc,
                null))
            .SingleOrDefaultAsync(cancellationToken);

        if (commerce is null)
        {
            return null;
        }

        var associatedUser = await _context.Users
            .AsNoTracking()
            .Where(user =>
                user.CommerceId == commerceId &&
                user.Role == Roles.Commerce)
            .Select(user => new AssociatedCommerceUserReadModel(
                user.Id,
                user.UserName,
                user.Email,
                user.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return commerce with { AssociatedUser = associatedUser };
    }

    public Task<Commerce?> GetForUpdateAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        return Entities.SingleOrDefaultAsync(
            commerce => commerce.Id == commerceId,
            cancellationToken);
    }
}
