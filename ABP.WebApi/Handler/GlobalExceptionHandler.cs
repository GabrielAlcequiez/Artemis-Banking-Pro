using System.Net;
using ABP.Application.Common.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace ABP.WebApi.Handler
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

            httpContext.Response.StatusCode = problemDetails.Status;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

            return true;
        }
    }
}
