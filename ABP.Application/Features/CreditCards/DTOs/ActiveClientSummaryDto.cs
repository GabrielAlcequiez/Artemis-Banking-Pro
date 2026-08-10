namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record ActiveClientSummaryDto(
    string Id,
    string Identification,
    string FullName,
    string Email);
