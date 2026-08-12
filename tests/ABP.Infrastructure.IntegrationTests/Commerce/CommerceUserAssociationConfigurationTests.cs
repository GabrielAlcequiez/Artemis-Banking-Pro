using ABP.Domain.Entities;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Commerces;

public sealed class CommerceUserAssociationConfigurationTests : IAsyncLifetime
{
    private readonly string _databaseName =
        $"ABP_CommerceUserAssociation_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;

    public CommerceUserAssociationConfigurationTests()
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
        _context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options);

        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Multiple_users_without_commerce_are_allowed()
    {
        _context.Users.AddRange(
            CreateUser("user-null-1", "00100000001", null),
            CreateUser("user-null-2", "00100000002", null));

        await _context.SaveChangesAsync();

        Assert.Equal(2, await _context.Users.CountAsync());
    }

    [Fact]
    public async Task Second_user_for_same_commerce_is_rejected()
    {
        var commerce = CreateCommerce();
        _context.Users.Add(CreateUser("commerce-user-1", "00200000001", commerce.Id));
        await _context.SaveChangesAsync();

        await using var secondContext = CreateContext();
        secondContext.Users.Add(CreateUser("commerce-user-2", "00200000002", commerce.Id));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task User_cannot_reference_a_missing_commerce()
    {
        _context.Users.Add(
            CreateUser("orphan-commerce", "00300000001", Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Associated_commerce_cannot_be_deleted()
    {
        var commerce = CreateCommerce();
        _context.Users.Add(
            CreateUser("restricted-delete", "00400000001", commerce.Id));
        await _context.SaveChangesAsync();

        await using var secondContext = CreateContext();
        var trackedCommerce = await secondContext.Commerces.FindAsync(commerce.Id);
        Assert.NotNull(trackedCommerce);
        secondContext.Commerces.Remove(trackedCommerce);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => secondContext.SaveChangesAsync());
    }

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options);

    private static User CreateUser(
        string userName,
        string identification,
        Guid? commerceId) =>
        new($"id-{userName}")
        {
            Name = "Usuario",
            LastName = "Comercio",
            Identification = identification,
            Email = $"{userName}@example.test",
            UserName = userName,
            IsActive = true,
            Role = commerceId.HasValue ? Roles.Commerce : Roles.Client,
            CommerceId = commerceId
        };

    private Commerce CreateCommerce()
    {
        var id = Guid.NewGuid();
        var suffix = id.ToString("N")[..8];
        var commerce = new Commerce
        {
            Name = "Comercio de prueba",
            Description = "Prueba de asociación uno a uno",
            Email = $"commerce-{suffix}@example.test",
            PhoneNumber = "8095551234",
            Rnc = suffix.PadLeft(9, '1')[..9],
            Status = CommerceStatus.Active
        };

        _context.Commerces.Add(commerce);
        return commerce;
    }
}

public sealed class CommerceUserAssociationModelTests
{
    [Fact]
    public void Model_configures_optional_unique_one_to_one_commerce_foreign_key()
    {
        using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;Database=ABP_CommerceMetadataOnly;" +
                    "Integrated Security=True;TrustServerCertificate=True;")
                .Options);
        var userType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userType);

        var index = Assert.Single(
            userType.GetIndexes(),
            candidate => candidate.Properties.Count == 1 &&
                         candidate.Properties[0].Name == nameof(User.CommerceId));
        Assert.True(index.IsUnique);
        Assert.Equal("[CommerceId] IS NOT NULL", index.GetFilter());

        var foreignKey = Assert.Single(
            userType.GetForeignKeys(),
            candidate => candidate.Properties.Count == 1 &&
                         candidate.Properties[0].Name == nameof(User.CommerceId));
        Assert.Equal(typeof(Commerce), foreignKey.PrincipalEntityType.ClrType);
        Assert.True(foreignKey.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.False(foreignKey.IsRequired);
    }
}
