using System.Reflection;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Entities.Lending;
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
        public DbSet<AccountToken> AccountTokens { get; set; }
        public DbSet<FinancialIdentifier> FinancialIdentifiers { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<CardConsumption> CardConsumptions { get; set; }
        public DbSet<CardPayment> CardPayments { get; set; }
        public DbSet<Commerce> Commerces { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanInstallment> LoanInstallments { get; set; }
        public DbSet<LoanPayment> LoanPayments { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}