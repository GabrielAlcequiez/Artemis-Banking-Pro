using System.Security.Claims;
using ABP.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace ABP.Shared.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public string? UserName =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? httpContextAccessor.HttpContext?.User.Identity?.Name;

    public Guid? CommerceId
    {
        get
        {
            var claimValue =
                httpContextAccessor.HttpContext?.User.FindFirstValue("CommerceId")
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("commerce_id");

            return Guid.TryParse(claimValue, out var commerceId)
                ? commerceId
                : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray()
        ?? [];

    public bool IsInRole(string role) =>
        !string.IsNullOrWhiteSpace(role) &&
        (httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false);
}
