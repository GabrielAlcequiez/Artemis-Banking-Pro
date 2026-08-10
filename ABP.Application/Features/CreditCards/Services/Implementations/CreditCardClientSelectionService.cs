using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CreditCardClientSelectionService(
    IActiveClientReader activeClientReader,
    ICustomerDebtSnapshotReader debtSnapshotReader,
    IValidator<CreditCardClientSearchRequest> requestValidator)
    : ICreditCardClientSelectionService
{
    public async Task<CreditCardClientSelectionResult> SearchAsync(
        CreditCardClientSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedRequest = request with
        {
            Identification = Normalize(request.Identification)
        };
        var clients = await activeClientReader.SearchAsync(
            normalizedRequest,
            cancellationToken);

        var candidates = await Task.WhenAll(
            clients.Data.Select(client => MapWithDebtAsync(client, cancellationToken)));
        var averageDebt = await debtSnapshotReader.GetAverageActiveClientDebtAsync(
            cancellationToken);

        var page = new PagedResult<CreditCardClientCandidateDto>(
            candidates,
            clients.Page,
            clients.PageSize,
            clients.TotalRecords);

        return new CreditCardClientSelectionResult(page, averageDebt);
    }

    public async Task<CreditCardClientCandidateDto?> GetActiveClientAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var client = await activeClientReader.GetByIdAsync(
            clientId,
            cancellationToken);

        return client is null
            ? null
            : await MapWithDebtAsync(client, cancellationToken);
    }

    private async Task<CreditCardClientCandidateDto> MapWithDebtAsync(
        ActiveClientSummaryDto client,
        CancellationToken cancellationToken)
    {
        var totalDebt = await debtSnapshotReader.GetTotalDebtAsync(
            client.Id,
            cancellationToken);

        return new CreditCardClientCandidateDto(
            client.Id,
            client.Identification,
            client.FullName,
            client.Email,
            totalDebt);
    }

    private static string? Normalize(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
