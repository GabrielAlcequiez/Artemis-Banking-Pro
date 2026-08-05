namespace ABP.Application.Interfaces.Services;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    string? UserName { get; }

    Guid? CommerceId { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);
}
