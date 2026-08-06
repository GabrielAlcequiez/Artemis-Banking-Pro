using ABP.Domain.Entities;

namespace ABP.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User, string>
    {
        
    }
}