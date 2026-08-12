using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.UnitTests.Features.Commerce;

internal sealed class CommerceUnitOfWorkStub : IUnitOfWork
{
    public Exception? SaveException { get; set; }

    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveCalls++;

        return SaveException is null
            ? Task.FromResult(1)
            : Task.FromException<int>(SaveException);
    }
}

internal sealed class CommerceCurrentUserStub : ICurrentUserService
{
    private CommerceCurrentUserStub(
        bool isAuthenticated,
        string? userId,
        IReadOnlyCollection<string> roles)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        Roles = roles;
    }

    public bool IsAuthenticated { get; }

    public string? UserId { get; }

    public string? UserName => null;

    public Guid? CommerceId => null;

    public IReadOnlyCollection<string> Roles { get; }

    public bool IsInRole(string role) => Roles.Contains(role);

    public static CommerceCurrentUserStub Administrator(string userId = "admin-1") =>
        new(true, userId, [RolesEnum.Administrator]);

    public static CommerceCurrentUserStub Client(string userId = "client-1") =>
        new(true, userId, [RolesEnum.Client]);

    private static class RolesEnum
    {
        public static readonly string Administrator =
            ABP.Domain.Enums.Roles.Administrator.ToString();

        public static readonly string Client =
            ABP.Domain.Enums.Roles.Client.ToString();
    }
}
