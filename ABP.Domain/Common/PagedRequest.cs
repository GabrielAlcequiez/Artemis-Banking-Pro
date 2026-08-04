namespace ABP.Domain.Common;

public sealed record PagedRequest(int Page = 1, int PageSize = 20);
