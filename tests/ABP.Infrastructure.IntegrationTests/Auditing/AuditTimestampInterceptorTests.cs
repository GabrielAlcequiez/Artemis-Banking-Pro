using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Auditing;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Auditing;

public sealed class AuditTimestampInterceptorTests
{
    [Fact]
    public void SaveChanges_sets_created_timestamp_for_a_new_entity()
    {
        var expectedTimestamp = new DateTimeOffset(2026, 8, 7, 14, 30, 0, TimeSpan.Zero);
        var clock = new StubClock(expectedTimestamp);
        using var context = CreateContext(clock);
        var user = CreateUser();

        context.Users.Add(user);
        context.SaveChanges();

        Assert.Equal(expectedTimestamp, user.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_sets_created_timestamp_for_a_new_entity()
    {
        var expectedTimestamp = new DateTimeOffset(2026, 8, 7, 15, 30, 0, TimeSpan.Zero);
        var clock = new StubClock(expectedTimestamp);
        await using var context = CreateContext(clock);
        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(expectedTimestamp, user.CreatedAtUtc);
        Assert.Null(user.LastModifiedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_sets_modified_timestamp_and_preserves_created_timestamp()
    {
        var createdTimestamp = new DateTimeOffset(2026, 8, 7, 15, 30, 0, TimeSpan.Zero);
        var modifiedTimestamp = createdTimestamp.AddHours(2);
        var clock = new StubClock(createdTimestamp);
        await using var context = CreateContext(clock);
        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        clock.UtcNow = modifiedTimestamp;
        user.Name = "Updated name";
        await context.SaveChangesAsync();

        Assert.Equal(createdTimestamp, user.CreatedAtUtc);
        Assert.Equal(modifiedTimestamp, user.LastModifiedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_preserves_an_explicit_created_timestamp()
    {
        var clockTimestamp = new DateTimeOffset(2026, 8, 7, 15, 30, 0, TimeSpan.Zero);
        var historicalTimestamp = clockTimestamp.AddYears(-1);
        var clock = new StubClock(clockTimestamp);
        await using var context = CreateContext(clock);
        var user = CreateUser();
        context.Users.Add(user);
        context.Entry(user)
            .Property<DateTimeOffset>(nameof(user.CreatedAtUtc))
            .CurrentValue = historicalTimestamp;

        await context.SaveChangesAsync();

        Assert.Equal(historicalTimestamp, user.CreatedAtUtc);
    }

    private static AppDbContext CreateContext(IClock clock)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditTimestampInterceptor(clock))
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
}
