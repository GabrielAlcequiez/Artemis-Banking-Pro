using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Auditing;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Auditing;

public sealed class AuditableEntityInterceptorTests
{
    [Fact]
    public void SaveChanges_sets_creation_audit_for_a_new_entity()
    {
        var expectedTimestamp = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.Zero);
        var currentUser = StubCurrentUser.Authenticated("admin-1");
        using var context = CreateContext(new StubClock(expectedTimestamp), currentUser);
        var user = CreateUser();

        context.Users.Add(user);
        context.SaveChanges();

        Assert.Equal(expectedTimestamp, user.CreatedAtUtc);
        Assert.Equal("admin-1", user.CreatedByUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_sets_creation_audit_without_modification_values()
    {
        var expectedTimestamp = new DateTimeOffset(2026, 8, 8, 15, 30, 0, TimeSpan.Zero);
        var currentUser = StubCurrentUser.Authenticated("admin-1");
        await using var context = CreateContext(new StubClock(expectedTimestamp), currentUser);
        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(expectedTimestamp, user.CreatedAtUtc);
        Assert.Equal("admin-1", user.CreatedByUserId);
        Assert.Null(user.LastModifiedAtUtc);
        Assert.Null(user.LastModifiedByUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_sets_latest_modification_actor_and_preserves_creation_audit()
    {
        var createdTimestamp = new DateTimeOffset(2026, 8, 8, 15, 30, 0, TimeSpan.Zero);
        var modifiedTimestamp = createdTimestamp.AddHours(2);
        var clock = new StubClock(createdTimestamp);
        var currentUser = StubCurrentUser.Authenticated("admin-1");
        await using var context = CreateContext(clock, currentUser);
        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        clock.UtcNow = modifiedTimestamp;
        currentUser.UserId = "cashier-2";
        user.Name = "Updated name";
        await context.SaveChangesAsync();

        Assert.Equal(createdTimestamp, user.CreatedAtUtc);
        Assert.Equal("admin-1", user.CreatedByUserId);
        Assert.Equal(modifiedTimestamp, user.LastModifiedAtUtc);
        Assert.Equal("cashier-2", user.LastModifiedByUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_preserves_explicit_historical_creation_audit()
    {
        var clockTimestamp = new DateTimeOffset(2026, 8, 8, 15, 30, 0, TimeSpan.Zero);
        var historicalTimestamp = clockTimestamp.AddYears(-1);
        var currentUser = StubCurrentUser.Authenticated("admin-1");
        await using var context = CreateContext(new StubClock(clockTimestamp), currentUser);
        var user = CreateUser();
        context.Users.Add(user);
        context.Entry(user)
            .Property<DateTimeOffset>(nameof(user.CreatedAtUtc))
            .CurrentValue = historicalTimestamp;
        context.Entry(user)
            .Property<string?>(nameof(user.CreatedByUserId))
            .CurrentValue = "legacy-user";

        await context.SaveChangesAsync();

        Assert.Equal(historicalTimestamp, user.CreatedAtUtc);
        Assert.Equal("legacy-user", user.CreatedByUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_keeps_actor_null_when_no_user_is_authenticated()
    {
        var expectedTimestamp = new DateTimeOffset(2026, 8, 8, 15, 30, 0, TimeSpan.Zero);
        var currentUser = new StubCurrentUser
        {
            IsAuthenticated = false,
            UserId = "untrusted-claim"
        };
        await using var context = CreateContext(new StubClock(expectedTimestamp), currentUser);
        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(expectedTimestamp, user.CreatedAtUtc);
        Assert.Null(user.CreatedByUserId);
    }

    private static AppDbContext CreateContext(
        IClock clock,
        ICurrentUserService currentUser)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(clock, currentUser))
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid().ToString())
        {
            Name = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "test.user",
            Identification = Guid.NewGuid().ToString("N"),
            Role = Roles.Client,
            IsActive = true
        };
    }

    private sealed class StubClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public bool IsAuthenticated { get; set; }

        public string? UserId { get; set; }

        public string? UserName => null;

        public Guid? CommerceId => null;

        public IReadOnlyCollection<string> Roles => [];

        public bool IsInRole(string role) => false;

        public static StubCurrentUser Authenticated(string userId) =>
            new()
            {
                IsAuthenticated = true,
                UserId = userId
            };
    }
}
