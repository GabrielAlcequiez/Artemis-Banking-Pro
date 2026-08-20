using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public sealed class UserPersistenceTests : IAsyncLifetime
{
    private readonly string _databaseName = $"ABP_UserPersistence_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;

    public UserPersistenceTests()
    {
        _connectionString = TestDatabase.CreateConnectionString(_databaseName);
    }

    public async Task InitializeAsync()
    {
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is null)
        {
            return;
        }

        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Theory]
    [InlineData("username")]
    [InlineData("email")]
    [InlineData("identification")]
    public async Task DbConstraint_DuplicateUsernameOrEmailOrCedula_ThrowsException(
        string duplicateField)
    {
        var first = CreateUser("first");
        await _context.Users.AddAsync(first);
        await _context.SaveChangesAsync();

        var second = CreateUser("second");
        switch (duplicateField)
        {
            case "username":
                second.UserName = first.UserName;
                break;
            case "email":
                second.Email = first.Email;
                break;
            case "identification":
                second.Identification = first.Identification;
                break;
        }

        await _context.Users.AddAsync(second);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task TokenPersistence_MarkedAsUsed_PersistsInDatabase()
    {
        var token = new AccountToken(Guid.NewGuid())
        {
            UserId = "identity-user-1",
            Purpose = AccountTokenPurpose.Activation,
            TokenHash = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };
        var repository = new AccountTokenRepository(_context);
        var usedAt = DateTimeOffset.UtcNow;

        await repository.AddAsync(token);

        var affectedRows = await repository.MarkAsUsedAsync(token.Id, usedAt);

        Assert.Equal(1, affectedRows);
        _context.ChangeTracker.Clear();
        var persisted = await _context.AccountTokens.SingleAsync(item => item.Id == token.Id);
        Assert.Equal(usedAt, persisted.UsedAtUtc);
        Assert.Equal(0, await repository.MarkAsUsedAsync(token.Id, usedAt.AddMinutes(1)));
    }

    private static User CreateUser(string suffix) => new($"user-{suffix}-{Guid.NewGuid():N}")
    {
        Name = "Test",
        LastName = "User",
        Email = $"{suffix}-{Guid.NewGuid():N}@test.com",
        UserName = $"user-{suffix}-{Guid.NewGuid():N}"[..20],
        Identification = Random.Shared.Next(100_000_000, 999_999_999).ToString(),
        Role = Roles.Client,
        IsActive = true
    };
}
