using ABP.Domain.Entities;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Identity.Services;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public sealed class CommerceUserInactivationServiceTests : IAsyncLifetime
{
    private readonly string _databaseName =
        $"ABP_CommerceUserInactivation_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _appContext = null!;
    private IdentityContext _identityContext = null!;

    public CommerceUserInactivationServiceTests()
    {
        var configuredServer = Environment.GetEnvironmentVariable("ABP_TEST_SQL_SERVER");
        var server = string.IsNullOrWhiteSpace(configuredServer)
            ? OperatingSystem.IsWindows()
                ? @"(localdb)\MSSQLLocalDB"
                : "localhost"
            : configuredServer;

        _connectionString =
            $"Server={server};Database={_databaseName};Integrated Security=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=true;";
    }

    public async Task InitializeAsync()
    {
        _appContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options);
        _identityContext = new IdentityContext(
            new DbContextOptionsBuilder<IdentityContext>()
                .UseSqlServer(_connectionString)
                .Options);

        var appCreator = _appContext.Database
            .GetService<IRelationalDatabaseCreator>();
        await appCreator.CreateAsync();
        await appCreator.CreateTablesAsync();

        var identityCreator = _identityContext.Database
            .GetService<IRelationalDatabaseCreator>();
        await identityCreator.CreateTablesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_appContext is not null)
        {
            await _appContext.Database.EnsureDeletedAsync();
            await _appContext.DisposeAsync();
        }

        if (_identityContext is not null)
        {
            await _identityContext.DisposeAsync();
        }
    }

    [Fact]
    public async Task Inactivation_commits_commerce_domain_user_and_identity_user_atomically()
    {
        var seeded = await SeedAsync();
        var commerce = await _appContext.Commerces.SingleAsync(
            item => item.Id == seeded.CommerceId);
        commerce.Status = CommerceStatus.Inactive;
        var service = CreateService(new UnitOfWork(_appContext));

        await service.InactivateAssociatedUsersAndCommitAsync(
            seeded.CommerceId);

        _appContext.ChangeTracker.Clear();
        _identityContext.ChangeTracker.Clear();
        var storedCommerce = await _appContext.Commerces
            .AsNoTracking()
            .SingleAsync(item => item.Id == seeded.CommerceId);
        var domainUser = await _appContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == seeded.UserId);
        var identityUser = await _identityContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == seeded.UserId);

        Assert.Equal(CommerceStatus.Inactive, storedCommerce.Status);
        Assert.False(domainUser.IsActive);
        Assert.False(identityUser.IsActive);
        Assert.False(identityUser.EmailConfirmed);
        Assert.NotEqual(seeded.SecurityStamp, identityUser.SecurityStamp);
    }

    [Fact]
    public async Task Failure_after_identity_save_rolls_back_identity_changes()
    {
        var seeded = await SeedAsync();
        var service = CreateService(new ThrowingUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InactivateAssociatedUsersAndCommitAsync(
                seeded.CommerceId));

        await using var verificationContext = new IdentityContext(
            new DbContextOptionsBuilder<IdentityContext>()
                .UseSqlServer(_connectionString)
                .Options);
        var identityUser = await verificationContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == seeded.UserId);

        Assert.True(identityUser.IsActive);
        Assert.True(identityUser.EmailConfirmed);
        Assert.Equal(seeded.SecurityStamp, identityUser.SecurityStamp);
    }

    private CommerceUserInactivationService CreateService(
        IUnitOfWork unitOfWork) =>
        new(
            unitOfWork,
            _identityContext,
            _appContext);

    private async Task<SeededData> SeedAsync()
    {
        var commerceId = Guid.NewGuid();
        const string userId = "commerce-user-1";
        const string securityStamp = "original-security-stamp";
        var commerce = new Commerce
        {
            Name = "Tienda Demo",
            Email = "contacto@tiendademo.com",
            PhoneNumber = "8095551234",
            Rnc = "101999999",
            Status = CommerceStatus.Active
        };
        var domainUser = new User(userId)
        {
            Name = "Usuario",
            LastName = "Comercio",
            Identification = "00100000001",
            Email = "commerce.user@example.test",
            UserName = "commerce.user",
            Role = Roles.Commerce,
            IsActive = true,
            CommerceId = commerceId
        };
        var identityUser = new AppUser
        {
            Id = userId,
            UserName = "commerce.user",
            NormalizedUserName = "COMMERCE.USER",
            Email = "commerce.user@example.test",
            NormalizedEmail = "COMMERCE.USER@EXAMPLE.TEST",
            IsActive = true,
            EmailConfirmed = true,
            SecurityStamp = securityStamp,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        _appContext.Commerces.Add(commerce);
        _appContext.Entry(commerce).Property(item => item.Id).CurrentValue = commerceId;
        _appContext.Users.Add(domainUser);
        _identityContext.Users.Add(identityUser);
        await _appContext.SaveChangesAsync();
        await _identityContext.SaveChangesAsync();
        _appContext.ChangeTracker.Clear();
        _identityContext.ChangeTracker.Clear();

        return new(commerceId, userId, securityStamp);
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated app database failure");
    }

    private sealed record SeededData(
        Guid CommerceId,
        string UserId,
        string SecurityStamp);
}
