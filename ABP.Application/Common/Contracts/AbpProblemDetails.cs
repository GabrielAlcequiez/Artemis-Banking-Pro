namespace ABP.Application.Common.Contracts;

public sealed class AbpProblemDetails
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    public string? TraceId { get; set; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }
}
