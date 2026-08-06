using ABP.Domain.Entities;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class UserRepository(AppDbContext context) : GenericRepository<User, string>(context), IUserRepository
    {
        
    }
}