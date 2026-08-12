using ABP.Domain.Enums;

namespace ABP.Application.Features.Commerce.DTOs;

public sealed record CommerceListRequest(
    int Page = 1,
    int PageSize = 20,
    CommerceStatusFilter? Status = null);
