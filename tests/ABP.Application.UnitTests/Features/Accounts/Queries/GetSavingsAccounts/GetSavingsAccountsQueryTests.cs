using ABP.Application.Features.Accounts.Queries.GetSavingsAccounts;
using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Accounts.Queries.GetSavingsAccounts
{
    public sealed class GetSavingsAccountsQueryTests
    {
        [Fact]
        public void Page_must_be_at_least_one()
        {
            var validator = new GetSavingsAccountsQueryValidator();
            var query = new GetSavingsAccountsQuery(new PagedRequest(0, 10), null, null, null);

            var result = validator.Validate(query);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Handler_maps_the_page_of_accounts()
        {
            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = "user-1",
                AccountNumber = "100000001",
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active,
                Balance = 500m
            };
            var repository = new StubSavingsAccountRepository
            {
                Page = new PagedResult<SavingsAccount>([account], 1, 10, 1)
            };
            var handler = new GetSavingsAccountsQueryHandler(repository, CreateMapper());

            var result = await handler.Handle(
                new GetSavingsAccountsQuery(new PagedRequest(1, 10), null, null, null), CancellationToken.None);

            Assert.Equal(1, result.TotalRecords);
            Assert.Single(result.Data);
            Assert.Equal("100000001", result.Data.First().AccountNumber);
        }

        private static IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddApplicationServices();

            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private sealed class StubSavingsAccountRepository : ISavingsAccountRepository
        {
            public PagedResult<SavingsAccount> Page { get; set; } = new([], 1, 10, 0);

            public Task<PagedResult<SavingsAccount>> GetPagedAsync(
                PagedRequest request, string? ownerIdentification = null, SavingsAccountStatus? status = null,
                SavingsAccountType? type = null, CancellationToken cancellationToken = default) =>
                Task.FromResult(Page);

            public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(
                string ownerUserId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount> AddAsync(SavingsAccount entity, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> UpdateAsync(Guid id, SavingsAccount value, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task<SavingsAccount?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
        }
    }
}
