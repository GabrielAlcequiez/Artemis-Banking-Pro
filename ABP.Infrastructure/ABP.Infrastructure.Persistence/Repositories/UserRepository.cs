using ABP.Domain.Entities;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class UserRepository(AppDbContext context) : GenericRepository<User, string>(context), IUserRepository
    {
        public Task<bool> GetByIdentificationAsync(string identification)
        {
            return _context.Users.AnyAsync(x => x.Identification == identification);
        }
    }
}