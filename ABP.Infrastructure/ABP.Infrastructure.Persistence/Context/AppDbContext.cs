using System.Reflection;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Context
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        #region DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }
        public DbSet<AccountTransaction> AccountTransactions { get; set; }
        public DbSet<Beneficiary> Beneficiaries { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}