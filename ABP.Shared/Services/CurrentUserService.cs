using System.Security.Claims;
using ABP.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace ABP.Shared.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

        public string? UserName =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        public Guid? CommerceId
        {
            get
            {
                var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst("CommerceId")?.Value
                                 ?? _httpContextAccessor.HttpContext?.User?.FindFirst("commerce_id")?.Value;

                return Guid.TryParse(claimValue, out var commerceId) ? commerceId : null;
            }
        }

        public IReadOnlyCollection<string> Roles
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return Array.Empty<string>();

                return user.FindAll(ClaimTypes.Role)
                           .Select(c => c.Value)
                           .Distinct()
                           .ToList()
                           .AsReadOnly();
            }
        }

        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }
    }
}