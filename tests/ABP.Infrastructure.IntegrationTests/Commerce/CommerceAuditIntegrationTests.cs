using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Auditing;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Commerces;

public sealed class CommerceAuditIntegrationTests
{
    [Fact]
    public async Task Create_and_update_record_the_authenticated_actors()
    {
        var createdAt = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var modifiedAt = createdAt.AddHours(1);
        var clock = new StubClock(createdAt);
        var currentUser = new StubCurrentUser("admin-1");
        await using var context = CreateContext(clock, currentUser);
        var commerce = new Commerce
        {
            Name = "Tienda Demo",
            Email = "contacto@tiendademo.com",
            PhoneNumber = "8095551234",
            Rnc = "101999999",
            Status = CommerceStatus.Active
        };

        context.Commerces.Add(commerce);
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, commerce.CreatedAtUtc);
        Assert.Equal("admin-1", commerce.CreatedByUserId);
        Assert.Null(commerce.LastModifiedAtUtc);

        clock.UtcNow = modifiedAt;
        currentUser.UserId = "admin-2";
        commerce.Name = "Tienda Actualizada";
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, commerce.CreatedAtUtc);
        Assert.Equal("admin-1", commerce.CreatedByUserId);
        Assert.Equal(modifiedAt, commerce.LastModifiedAtUtc);
        Assert.Equal("admin-2", commerce.LastModifiedByUserId);
    }

    private static AppDbContext CreateContext(
        IClock clock,
        ICurrentUserService currentUser)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CommerceAudit_{Guid.NewGuid():N}")
            .AddInterceptors(new AuditableEntityInterceptor(clock, currentUser))
            .Options;

        return new AppDbContext(options);
    }

    private sealed class StubClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class StubCurrentUser(string userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;

        public string? UserId { get; set; } = userId;

        public string? UserName => null;

        public Guid? CommerceId => null;

        public IReadOnlyCollection<string> Roles =>
            [ABP.Domain.Enums.Roles.Administrator.ToString()];

        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
