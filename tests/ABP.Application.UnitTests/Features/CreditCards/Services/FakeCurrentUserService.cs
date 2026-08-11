using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public bool IsAuthenticated { get; init; } = true;

    public string? UserId { get; init; } = "admin-1";

    public string? UserName { get; init; } = "Test administrator";

    public Guid? CommerceId => null;

    public IReadOnlyCollection<string> Roles { get; init; } =
        [ABP.Domain.Enums.Roles.Administrator.ToString()];

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}
