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
    }
}
