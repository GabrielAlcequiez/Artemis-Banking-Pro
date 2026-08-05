namespace ABP.Application.Common.Interfaces.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset Now { get; }

    DateOnly Today { get; }
}
