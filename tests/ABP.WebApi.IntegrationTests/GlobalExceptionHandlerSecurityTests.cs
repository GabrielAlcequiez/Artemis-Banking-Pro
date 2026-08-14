using ABP.Application.Exceptions;
using ABP.WebApi.Handler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ABP.WebApi.IntegrationTests;

public sealed class GlobalExceptionHandlerSecurityTests
{
    [Fact]
    public async Task Persistence_exception_does_not_write_pan_to_logs_or_response()
    {
        const string fullPan = "4000000000001234";
        var logger = new CapturingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/credit-card";
        context.Response.Body = new MemoryStream();

        await handler.TryHandleAsync(
            context,
            new PersistenceConflictException(
                new InvalidOperationException(fullPan)),
            CancellationToken.None);
        context.Response.Body.Position = 0;
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.DoesNotContain(fullPan, response);
        Assert.DoesNotContain(fullPan, string.Join(' ', logger.Messages));
        Assert.All(logger.Exceptions, Assert.Null);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
