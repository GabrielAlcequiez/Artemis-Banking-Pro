using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Interfaces;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CreditCardClientSelectionService(
    IUserRepository userRepository,
    ICustomerDebtService customerDebtService,
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
        var clients = await userRepository.GetActiveClientsPagedAsync(
            new PagedRequest(normalizedRequest.Page, normalizedRequest.PageSize),
            normalizedRequest.Identification,
            cancellationToken);

        var clientDebts = await customerDebtService.GetTotalDebtsAsync(
            clients.Data.Select(client => client.Id).ToArray(),
            cancellationToken);
        var candidates = clients.Data
            .Select(client => MapWithDebt(client, clientDebts))
            .ToArray();
        var averageDebt = await customerDebtService.GetAverageActiveClientDebtAsync(
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

        var client = await userRepository.GetActiveClientByIdAsync(
            clientId,
            cancellationToken);

        return client is null
            ? null
            : await MapWithDebtAsync(client, cancellationToken);
    }

    private async Task<CreditCardClientCandidateDto> MapWithDebtAsync(
        User client,
        CancellationToken cancellationToken)
    {
        var totalDebt = await customerDebtService.GetTotalDebtAsync(
            client.Id,
            cancellationToken);

        return new CreditCardClientCandidateDto(
            client.Id,
            client.Identification,
            $"{client.Name} {client.LastName}".Trim(),
            client.Email,
            totalDebt);
    }

    private static CreditCardClientCandidateDto MapWithDebt(
        User client,
        IReadOnlyDictionary<string, decimal> clientDebts)
    {
        return new CreditCardClientCandidateDto(
            client.Id,
            client.Identification,
            $"{client.Name} {client.LastName}".Trim(),
            client.Email,
            clientDebts.GetValueOrDefault(client.Id));
    }

    private static string? Normalize(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
