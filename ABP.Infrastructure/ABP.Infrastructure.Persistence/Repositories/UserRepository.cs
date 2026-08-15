using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class UserRepository(AppDbContext context) : GenericRepository<User, string>(context), IUserRepository
    {
        public Task<User?> FindByIdentificationAsync(string identification)
        {
            return _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Identification == identification);
        }

        public async Task<PagedResult<User>> GetPagedAsync(
            PagedRequest request,
            bool commerceOnly = false,
            Roles? role = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsNoTracking();

            if (role is not null)
            {
                query = query.Where(x => x.Role == role);
            }
            else if (commerceOnly)
            {
                query = query.Where(x => x.Role == Roles.Commerce);
            }
            else
            {
                query = query.Where(x => x.Role != Roles.Commerce);
            }

            query = query.OrderByDescending(x => x.CreatedAtUtc);

            var totalRecords = await query.CountAsync(cancellationToken);

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<User>(data, request.Page, request.PageSize, totalRecords);
        }

        public async Task<PagedResult<User>> GetActiveClientsPagedAsync(
            PagedRequest request,
            string? identification = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalizedPage = Math.Max(request.Page, 1);
            var normalizedPageSize = Math.Clamp(request.PageSize, 1, 20);
            var normalizedIdentification = identification?.Trim();

            var query = _context.Users
                .AsNoTracking()
                .Where(user => user.Role == Roles.Client && user.IsActive);

            if (!string.IsNullOrWhiteSpace(normalizedIdentification))
            {
                query = query.Where(user => user.Identification == normalizedIdentification);
            }

            var totalRecords = await query.CountAsync(cancellationToken);
            var skip = (int)Math.Min(
                (long)(normalizedPage - 1) * normalizedPageSize,
                int.MaxValue);
            var data = await query
                .OrderBy(user => user.Identification)
                .Skip(skip)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<User>(
                data,
                normalizedPage,
                normalizedPageSize,
                totalRecords);
        }

        public Task<User?> GetActiveClientByIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == clientId
                        && user.Role == Roles.Client
                        && user.IsActive,
                    cancellationToken);
        }

        public Task<int> CountActiveClientsAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .CountAsync(
                    user => user.Role == Roles.Client && user.IsActive,
                    cancellationToken);
        }

        public Task<int> CountInactiveClientsAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .CountAsync(
                    user => user.Role == Roles.Client && !user.IsActive,
                    cancellationToken);
        }

        public Task<bool> ExistsByCommerceIdAsync(
            Guid commerceId,
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .AnyAsync(
                    user => user.CommerceId == commerceId,
                    cancellationToken);
        }
    }
}
