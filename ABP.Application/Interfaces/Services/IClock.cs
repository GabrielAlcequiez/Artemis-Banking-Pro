namespace ABP.Application.Interfaces.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset Now { get; }

    DateOnly Today { get; }
}
