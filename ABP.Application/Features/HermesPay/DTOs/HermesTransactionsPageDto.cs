namespace ABP.Application.Features.HermesPay.DTOs;

public sealed record HermesTransactionsPageDto(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    Guid CommerceId,
    string CommerceName,
    IReadOnlyCollection<HermesTransactionDto> Data);
