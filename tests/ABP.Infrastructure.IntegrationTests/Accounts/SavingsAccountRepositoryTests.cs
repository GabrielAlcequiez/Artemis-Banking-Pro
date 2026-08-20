using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Accounts;

public sealed class SavingsAccountRepositoryTests : IAsyncLifetime
{
    #region Test setup

    private readonly string _databaseName = $"ABP_SavingsAccountRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;
    private SavingsAccountRepository _repository = null!;

    public SavingsAccountRepositoryTests()
    {
        _connectionString = TestDatabase.CreateConnectionString(_databaseName);
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new SavingsAccountRepository(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    #endregion

    #region Round trip

    [Fact]
    public async Task AddAsync_and_GetByIdAsync_round_trip_the_account()
    {
        await SeedOwnersAsync(_context);
        var account = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "client-1",
            AccountNumber = "900000001",
            Balance = 1500m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };

        await _repository.AddAsync(account);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(account.Id);

        Assert.NotNull(result);
        Assert.Equal("900000001", result.AccountNumber);
        Assert.Equal("client-1", result.OwnerUserId);
        Assert.Equal(1500m, result.Balance);
        Assert.Equal(SavingsAccountType.Principal, result.Type);
        Assert.Equal(SavingsAccountStatus.Active, result.Status);
    }

    #endregion

    #region GetByAccountNumberAsync

    [Fact]
    public async Task GetByAccountNumberAsync_returns_existing_account()
    {
        var seeded = await SeedAccountsAsync(_context);

        var result = await _repository.GetByAccountNumberAsync(seeded.Client1Principal.AccountNumber);

        Assert.NotNull(result);
        Assert.Equal(seeded.Client1Principal.Id, result.Id);
    }

    [Fact]
    public async Task GetByAccountNumberAsync_returns_null_when_account_does_not_exist()
    {
        await SeedAccountsAsync(_context);

        var result = await _repository.GetByAccountNumberAsync("000000000");

        Assert.Null(result);
    }

    #endregion

    #region GetPrincipalAccountAsync

    [Fact]
    public async Task GetPrincipalAccountAsync_returns_the_principal_and_ignores_secondary_accounts()
    {
        var seeded = await SeedAccountsAsync(_context);

        var result = await _repository.GetPrincipalAccountAsync("client-1");

        Assert.NotNull(result);
        Assert.Equal(seeded.Client1Principal.Id, result.Id);
        Assert.Equal(SavingsAccountType.Principal, result.Type);
    }

    #endregion

    #region AccountNumberExistsAsync

    [Fact]
    public async Task AccountNumberExistsAsync_returns_expected_result()
    {
        var seeded = await SeedAccountsAsync(_context);

        var existingResult = await _repository.AccountNumberExistsAsync(seeded.Client1Principal.AccountNumber);
        var missingResult = await _repository.AccountNumberExistsAsync("000000000");

        Assert.True(existingResult);
        Assert.False(missingResult);
    }

    #endregion

    #region GetPagedAsync filters

    [Fact]
    public async Task GetPagedAsync_filters_by_status()
    {
        await SeedAccountsAsync(_context);

        var result = await _repository.GetPagedAsync(new PagedRequest(1, 20), status: SavingsAccountStatus.Active);

        Assert.Equal(3, result.TotalRecords);
        Assert.All(result.Data, account => Assert.Equal(SavingsAccountStatus.Active, account.Status));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_type()
    {
        await SeedAccountsAsync(_context);

        var result = await _repository.GetPagedAsync(new PagedRequest(1, 20), type: SavingsAccountType.Principal);

        Assert.Equal(2, result.TotalRecords);
        Assert.All(result.Data, account => Assert.Equal(SavingsAccountType.Principal, account.Type));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_owner_identification_via_user_join()
    {
        await SeedAccountsAsync(_context);

        var result = await _repository.GetPagedAsync(new PagedRequest(1, 20), ownerIdentification: "11111111111");

        Assert.Equal(3, result.TotalRecords);
        Assert.All(result.Data, account => Assert.Equal("client-1", account.OwnerUserId));
    }

    [Fact]
    public async Task GetPagedAsync_respects_pagination_and_reports_total_records()
    {
        await SeedAccountsAsync(_context);

        var result = await _repository.GetPagedAsync(new PagedRequest(2, 2));

        Assert.Equal(5, result.TotalRecords);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(
            ["100000003", "200000001"],
            result.Data.Select(account => account.AccountNumber).ToArray());
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_persists_balance_and_status_changes()
    {
        var seeded = await SeedAccountsAsync(_context);

        var updated = new SavingsAccount(seeded.Client1Principal.Id)
        {
            OwnerUserId = seeded.Client1Principal.OwnerUserId,
            AccountNumber = seeded.Client1Principal.AccountNumber,
            Balance = 999.50m,
            Type = seeded.Client1Principal.Type,
            Status = SavingsAccountStatus.Cancelled
        };

        await _repository.UpdateAsync(seeded.Client1Principal.Id, updated);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(seeded.Client1Principal.Id);

        Assert.NotNull(result);
        Assert.Equal(999.50m, result.Balance);
        Assert.Equal(SavingsAccountStatus.Cancelled, result.Status);
    }

    #endregion

    #region Test data builders

    private static async Task SeedOwnersAsync(AppDbContext context)
    {
        context.Users.Add(new User("client-1")
        {
            Name = "María",
            LastName = "Gómez",
            Identification = "11111111111",
            Email = "maria@example.test",
            UserName = "maria",
            IsActive = true,
            Role = Roles.Client
        });

        await context.SaveChangesAsync();
    }

    private static async Task<SeededAccounts> SeedAccountsAsync(AppDbContext context)
    {
        context.Users.AddRange(
            new User("client-1")
            {
                Name = "María",
                LastName = "Gómez",
                Identification = "11111111111",
                Email = "maria@example.test",
                UserName = "maria",
                IsActive = true,
                Role = Roles.Client
            },
            new User("client-2")
            {
                Name = "Pedro",
                LastName = "Díaz",
                Identification = "22222222222",
                Email = "pedro@example.test",
                UserName = "pedro",
                IsActive = true,
                Role = Roles.Client
            });

        var client1Principal = AddAccount(context, "client-1", "100000001", SavingsAccountType.Principal, SavingsAccountStatus.Active, 1000m);
        var client1Secondary = AddAccount(context, "client-1", "100000002", SavingsAccountType.Secondary, SavingsAccountStatus.Active, 200m);
        var client1SecondaryCancelled = AddAccount(context, "client-1", "100000003", SavingsAccountType.Secondary, SavingsAccountStatus.Cancelled, 0m);
        var client2Principal = AddAccount(context, "client-2", "200000001", SavingsAccountType.Principal, SavingsAccountStatus.Active, 500m);
        var client2SecondaryCancelled = AddAccount(context, "client-2", "200000002", SavingsAccountType.Secondary, SavingsAccountStatus.Cancelled, 0m);

        await context.SaveChangesAsync();

        return new(client1Principal, client1Secondary, client1SecondaryCancelled, client2Principal, client2SecondaryCancelled);
    }

    private static SavingsAccount AddAccount(
        AppDbContext context,
        string ownerUserId,
        string accountNumber,
        SavingsAccountType type,
        SavingsAccountStatus status,
        decimal balance)
    {
        var account = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = ownerUserId,
            AccountNumber = accountNumber,
            Balance = balance,
            Type = type,
            Status = status
        };

        context.SavingsAccounts.Add(account);
        return account;
    }

    private sealed record SeededAccounts(
        SavingsAccount Client1Principal,
        SavingsAccount Client1Secondary,
        SavingsAccount Client1SecondaryCancelled,
        SavingsAccount Client2Principal,
        SavingsAccount Client2SecondaryCancelled);

    #endregion
}
