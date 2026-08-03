namespace ABP.Application.Common;

public sealed class PagedResult<TValue>(
    IReadOnlyCollection<TValue> data,
    int page,
    int pageSize,
    int totalRecords)
{

    public IReadOnlyCollection<TValue> Data { get; } = data;

    public int Page { get; } = page;

    public int PageSize { get; } = pageSize;

    public int TotalRecords { get; } = totalRecords;

    public int TotalPages { get; } = pageSize > 0
            ? (int)Math.Ceiling(totalRecords / (double)pageSize)
            : 0;
}
