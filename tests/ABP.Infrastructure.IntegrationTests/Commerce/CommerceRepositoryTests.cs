using ABP.Application.Exceptions;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Commerces;

public sealed class CommerceRepositoryTests : IAsyncLifetime
{
    private readonly string _databaseName = $"ABP_CommerceRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;

    public CommerceRepositoryTests()
    {
        var configuredServer = Environment.GetEnvironmentVariable("ABP_TEST_SQL_SERVER");
        var server = string.IsNullOrWhiteSpace(configuredServer)
            ? OperatingSystem.IsWindows()
                ? @"(localdb)\MSSQLLocalDB"
                : "localhost"
            : configuredServer;

        _connectionString = $"Server={server};Database={_databaseName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new AppDbContext(options);
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
    public async Task Default_search_returns_only_active_commerces_in_descending_created_order()
    {
        var seeded = await SeedAsync(_context);

        var result = await CreateRepository().SearchAsync(1, 20);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.ActiveOld.Id],
            result.Data.Select(commerce => commerce.Id).ToArray());
        Assert.All(result.Data, commerce => Assert.Equal(CommerceStatus.Active, commerce.Status));
        Assert.True(result.Data.Single(commerce => commerce.Id == seeded.ActiveNew.Id).HasAssociatedUser);
        Assert.False(result.Data.Single(commerce => commerce.Id == seeded.ActiveOld.Id).HasAssociatedUser);
    }

    [Fact]
    public async Task Search_filters_by_status_and_applies_pagination()
    {
        var seeded = await SeedAsync(_context);

        var inactive = await CreateRepository().SearchAsync(
            1,
            20,
            CommerceStatusFilter.Inactive);
        var secondActivePage = await CreateRepository().SearchAsync(
            2,
            1,
            CommerceStatusFilter.Active);

        Assert.Equal(seeded.Inactive.Id, inactive.Data.Single().Id);
        Assert.Equal(1, inactive.TotalRecords);
        Assert.Equal(seeded.ActiveOld.Id, secondActivePage.Data.Single().Id);
        Assert.Equal(2, secondActivePage.TotalRecords);
        Assert.Equal(2, secondActivePage.TotalPages);
    }

    [Fact]
    public async Task Search_all_returns_every_status_and_normalizes_page_values()
    {
        var seeded = await SeedAsync(_context);

        var result = await CreateRepository().SearchAsync(
            0,
            50,
            CommerceStatusFilter.All);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(
            [seeded.Inactive.Id, seeded.ActiveNew.Id, seeded.ActiveOld.Id],
            result.Data.Select(commerce => commerce.Id).ToArray());
    }

    [Fact]
    public async Task Search_page_outside_range_returns_empty_data_with_total_records()
    {
        await SeedAsync(_context);

        var result = await CreateRepository().SearchAsync(
            99,
            20,
            CommerceStatusFilter.All);

        Assert.Equal(3, result.TotalRecords);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Search_has_associated_user_ignores_non_commerce_roles()
    {
        var seeded = await SeedAsync(_context);
        _context.Users.Add(new User("client-associated-to-commerce")
        {
            Name = "Cliente",
            LastName = "Incorrecto",
            Identification = "00300000000",
            Email = "client-associated@example.test",
            UserName = "client-associated",
            IsActive = true,
            Role = Roles.Client,
            CommerceId = seeded.ActiveOld.Id
        });
        await _context.SaveChangesAsync();

        var result = await CreateRepository().SearchAsync(1, 20);

        Assert.False(result.Data.Single(commerce => commerce.Id == seeded.ActiveOld.Id).HasAssociatedUser);
    }

    [Fact]
    public async Task Get_details_returns_commerce_and_associated_commerce_user()
    {
        var seeded = await SeedAsync(_context);

        var result = await CreateRepository().GetDetailsAsync(seeded.ActiveNew.Id);

        Assert.NotNull(result);
        Assert.Equal("Comercio Activo Nuevo", result.Name);
        Assert.Equal(CommerceStatus.Active, result.Status);
        Assert.NotNull(result.AssociatedUser);
        Assert.Equal("commerce-user", result.AssociatedUser.Id);
        Assert.Equal("commerce.user@example.test", result.AssociatedUser.Email);
        Assert.True(result.AssociatedUser.IsActive);
    }

    [Fact]
    public async Task Get_details_returns_null_associated_user_when_commerce_has_none()
    {
        var seeded = await SeedAsync(_context);

        var result = await CreateRepository().GetDetailsAsync(seeded.ActiveOld.Id);

        Assert.NotNull(result);
        Assert.Null(result.AssociatedUser);
    }

    [Fact]
    public async Task Get_details_returns_null_when_commerce_does_not_exist()
    {
        await SeedAsync(_context);

        var result = await CreateRepository().GetDetailsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Email_exists_honors_normalization_and_excluded_commerce()
    {
        var seeded = await SeedAsync(_context);
        var repository = CreateRepository();

        var existing = await repository.EmailExistsAsync(" active.new@example.test ");
        var excluded = await repository.EmailExistsAsync(
            "active.new@example.test",
            seeded.ActiveNew.Id);
        var missing = await repository.EmailExistsAsync("missing@example.test");

        Assert.True(existing);
        Assert.False(excluded);
        Assert.False(missing);
    }

    [Fact]
    public async Task Rnc_exists_honors_normalization_and_excluded_commerce()
    {
        var seeded = await SeedAsync(_context);
        var repository = CreateRepository();

        var existing = await repository.RncExistsAsync(" 100000001 ");
        var excluded = await repository.RncExistsAsync("100000001", seeded.ActiveNew.Id);
        var missing = await repository.RncExistsAsync("999999999");

        Assert.True(existing);
        Assert.False(excluded);
        Assert.False(missing);
    }

    [Fact]
    public async Task Unique_email_index_rejects_duplicate_values()
    {
        var seeded = await SeedAsync(_context);
        AddCommerce(
            _context,
            "Email Duplicado",
            seeded.ActiveNew.Email,
            "200000001",
            CommerceStatus.Active,
            new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Unique_rnc_index_rejects_duplicate_values()
    {
        var seeded = await SeedAsync(_context);
        AddCommerce(
            _context,
            "RNC Duplicado",
            "unique@example.test",
            seeded.ActiveNew.Rnc,
            CommerceStatus.Active,
            new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Unit_of_work_translates_unique_index_race_to_persistence_conflict()
    {
        var seeded = await SeedAsync(_context);
        AddCommerce(
            _context,
            "Email Duplicado Concurrente",
            seeded.ActiveNew.Email,
            "200000002",
            CommerceStatus.Active,
            new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));
        var unitOfWork = new UnitOfWork(_context);

        var exception = await Assert.ThrowsAsync<PersistenceConflictException>(
            () => unitOfWork.SaveChangesAsync());

        Assert.IsType<DbUpdateException>(exception.InnerException);
    }

    [Fact]
    public async Task Get_for_update_tracks_commerce_and_persists_changes()
    {
        var seeded = await SeedAsync(_context);
        var repository = CreateRepository();

        var commerce = await repository.GetForUpdateAsync(seeded.ActiveNew.Id);

        Assert.NotNull(commerce);
        Assert.Equal(EntityState.Unchanged, _context.Entry(commerce).State);

        commerce.Name = "Nombre Actualizado";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var persisted = await repository.GetByIdAsync(seeded.ActiveNew.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Nombre Actualizado", persisted.Name);
    }

    [Fact]
    public async Task Concurrent_updates_throw_db_update_concurrency_exception()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new CommerceRepository(firstContext);
        var secondRepository = new CommerceRepository(secondContext);

        var firstCommerce = await firstRepository.GetForUpdateAsync(seeded.ActiveNew.Id);
        var secondCommerce = await secondRepository.GetForUpdateAsync(seeded.ActiveNew.Id);
        Assert.NotNull(firstCommerce);
        Assert.NotNull(secondCommerce);

        firstCommerce.Name = "Primera Actualización";
        await firstContext.SaveChangesAsync();

        secondCommerce.Name = "Segunda Actualización";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    private CommerceRepository CreateRepository() => new(_context);

    private static async Task<SeededCommerces> SeedAsync(AppDbContext context)
    {
        var activeOld = AddCommerce(
            context,
            "Comercio Activo Antiguo",
            "active.old@example.test",
            "100000000",
            CommerceStatus.Active,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
        var activeNew = AddCommerce(
            context,
            "Comercio Activo Nuevo",
            "active.new@example.test",
            "100000001",
            CommerceStatus.Active,
            new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero));
        var inactive = AddCommerce(
            context,
            "Comercio Inactivo",
            "inactive@example.test",
            "100000002",
            CommerceStatus.Inactive,
            new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero));

        context.Users.Add(new User("commerce-user")
        {
            Name = "Usuario",
            LastName = "Comercio",
            Identification = "00400000000",
            Email = "commerce.user@example.test",
            UserName = "commerce-user",
            IsActive = true,
            Role = Roles.Commerce,
            CommerceId = activeNew.Id
        });

        await context.SaveChangesAsync();
        return new(activeOld, activeNew, inactive);
    }

    private static Commerce AddCommerce(
        AppDbContext context,
        string name,
        string email,
        string rnc,
        CommerceStatus status,
        DateTimeOffset createdAt)
    {
        var commerce = new Commerce
        {
            Name = name,
            Description = $"Descripción de {name}",
            Email = email,
            PhoneNumber = "8095551234",
            Rnc = rnc,
            Status = status
        };

        context.Commerces.Add(commerce);
        context.Entry(commerce).Property(entity => entity.Id).CurrentValue = Guid.NewGuid();
        context.Entry(commerce).Property(entity => entity.CreatedAtUtc).CurrentValue = createdAt;
        return commerce;
    }

    private sealed record SeededCommerces(
        Commerce ActiveOld,
        Commerce ActiveNew,
        Commerce Inactive);
}
