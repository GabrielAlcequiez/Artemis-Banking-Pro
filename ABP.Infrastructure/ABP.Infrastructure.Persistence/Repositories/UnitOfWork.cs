using ABP.Application.Exceptions;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        private readonly AppDbContext _context = context;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new FinancialConcurrencyException(exception);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                throw new PersistenceConflictException(exception);
            }
            catch (DbUpdateException exception)
            {
                throw new PersistenceFailureException(exception);
            }
        }

        private static bool IsUniqueConstraintViolation(
            DbUpdateException exception) =>
            exception.InnerException is SqlException
            {
                Number: 2601 or 2627
            };
    }
}
