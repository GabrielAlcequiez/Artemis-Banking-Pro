using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Accounts;

public sealed class BeneficiaryRepositoryTests : IAsyncLifetime
{
    #region Test setup

    private readonly string _databaseName = $"ABP_BeneficiaryRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;
    private BeneficiaryRepository _repository = null!;

    public BeneficiaryRepositoryTests()
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

        _repository = new BeneficiaryRepository(_context);
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

    #region AddAsync + GetByOwnerAsync

    [Fact]
    public async Task AddAsync_and_GetByOwnerAsync_round_trip_the_beneficiaries()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetByOwnerAsync("owner-1");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.BeneficiaryAccountId == seeded.AccountX.Id);
        Assert.Contains(result, b => b.BeneficiaryAccountId == seeded.AccountY.Id);
    }

    [Fact]
    public async Task GetByOwnerAsync_returns_empty_for_an_owner_without_beneficiaries()
    {
        await SeedAsync(_context);

        var result = await _repository.GetByOwnerAsync("owner-without-beneficiaries");

        Assert.Empty(result);
    }

    #endregion

    #region GetAsync

    [Fact]
    public async Task GetAsync_returns_the_beneficiary_for_the_owner_and_account()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetAsync("owner-1", seeded.AccountX.Id);

        Assert.NotNull(result);
        Assert.Equal(seeded.BeneficiaryOwner1ToX.Id, result.Id);
    }

    [Fact]
    public async Task GetAsync_returns_null_when_the_account_is_not_a_beneficiary_of_the_owner()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetAsync("owner-1", seeded.AccountZ.Id);

        Assert.Null(result);
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_returns_expected_result()
    {
        var seeded = await SeedAsync(_context);

        var existingResult = await _repository.ExistsAsync("owner-1", seeded.AccountX.Id);
        var missingResult = await _repository.ExistsAsync("owner-1", seeded.AccountZ.Id);

        Assert.True(existingResult);
        Assert.False(missingResult);
    }

    #endregion

    #region Test data builders

    private static async Task<SeededBeneficiaries> SeedAsync(AppDbContext context)
    {
        context.Users.AddRange(
            new User("owner-1")
            {
                Name = "María",
                LastName = "Gómez",
                Identification = "11111111111",
                Email = "maria@example.test",
                UserName = "maria",
                IsActive = true,
                Role = Roles.Client
            },
            new User("owner-without-beneficiaries")
            {
                Name = "Pedro",
                LastName = "Díaz",
                Identification = "22222222222",
                Email = "pedro@example.test",
                UserName = "pedro",
                IsActive = true,
                Role = Roles.Client
            },
            new User("account-holder")
            {
                Name = "Ana",
                LastName = "Pérez",
                Identification = "33333333333",
                Email = "ana@example.test",
                UserName = "ana",
                IsActive = true,
                Role = Roles.Client
            });

        var accountX = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "account-holder",
            AccountNumber = "400000001",
            Balance = 100m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        var accountY = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "account-holder",
            AccountNumber = "400000002",
            Balance = 200m,
            Type = SavingsAccountType.Secondary,
            Status = SavingsAccountStatus.Active
        };
        var accountZ = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = "owner-without-beneficiaries",
            AccountNumber = "400000003",
            Balance = 300m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        context.SavingsAccounts.AddRange(accountX, accountY, accountZ);

        var beneficiaryOwner1ToX = new Beneficiary(Guid.NewGuid())
        {
            OwnerUserId = "owner-1",
            BeneficiaryAccountId = accountX.Id
        };
        var beneficiaryOwner1ToY = new Beneficiary(Guid.NewGuid())
        {
            OwnerUserId = "owner-1",
            BeneficiaryAccountId = accountY.Id
        };
        context.Beneficiaries.AddRange(beneficiaryOwner1ToX, beneficiaryOwner1ToY);

        await context.SaveChangesAsync();

        return new(accountX, accountY, accountZ, beneficiaryOwner1ToX, beneficiaryOwner1ToY);
    }

    private sealed record SeededBeneficiaries(
        SavingsAccount AccountX,
        SavingsAccount AccountY,
        SavingsAccount AccountZ,
        Beneficiary BeneficiaryOwner1ToX,
        Beneficiary BeneficiaryOwner1ToY);

    #endregion
}
