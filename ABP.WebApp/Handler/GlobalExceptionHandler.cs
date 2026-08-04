using System.Net;
using ABP.Application.Common.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace ABP.WebApp.Handler
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred for request {Path} with correlation id {CorrelationId}",
                httpContext.Request.Path, httpContext.TraceIdentifier);

            var problemDetails = ProblemDetailsFactory.Create(
                exception,
                httpContext.TraceIdentifier,
                httpContext.Request.Path);

            if (WantsHtml(httpContext.Request))
            {
                httpContext.Response.Redirect("/Home/Error");
                return true;
            }

            httpContext.Response.StatusCode = problemDetails.Status;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

            return true;
        }

        private static bool WantsHtml(HttpRequest request)
        {
            var accept = request.GetTypedHeaders().Accept;
            if (accept is null)
            {
                return false;
            }

            foreach (var header in accept)
            {
                if (header.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
