using Microsoft.AspNetCore.Builder;

namespace ABP.Shared.Middleware
{
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
