using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using FluentValidation;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanClientSelectionService(
    ILoanRepository repository,
    ICustomerDebtService customerDebtService,
    IValidator<LoanClientSearchRequest> requestValidator)
    : ILoanClientSelectionService
{
    public async Task<LoanClientSelectionResult> SearchAsync(
        LoanClientSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var identification = Normalize(request.Identification);
        var clients = await repository.GetEligibleClientsPagedAsync(
            new PagedRequest(request.Page, request.PageSize),
            identification,
            cancellationToken);
        var clientDebts = await customerDebtService.GetTotalDebtsAsync(
            clients.Data.Select(client => client.Id).ToArray(),
            cancellationToken);
        var candidates = clients.Data
            .Select(client => MapWithDebt(client, clientDebts))
            .ToArray();
        var averageDebt = await customerDebtService.GetAverageActiveClientDebtAsync(
            cancellationToken);

        var page = new PagedResult<LoanClientCandidateDto>(
            candidates,
            clients.Page,
            clients.PageSize,
            clients.TotalRecords);

        return new LoanClientSelectionResult(page, averageDebt);
    }

    public async Task<LoanClientCandidateDto?> GetEligibleClientAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var client = await repository.GetEligibleClientByIdAsync(
            clientId,
            cancellationToken);

        if (client is null)
        {
            return null;
        }

        var currentDebt = await customerDebtService.GetTotalDebtAsync(
            client.Id,
            cancellationToken);

        return new LoanClientCandidateDto(
            client.Id,
            client.Identification,
            client.FullName,
            client.Email,
            currentDebt);
    }

    private static LoanClientCandidateDto MapWithDebt(
        LoanClientCandidateReadModel client,
        IReadOnlyDictionary<string, decimal> clientDebts) =>
        new(
            client.Id,
            client.Identification,
            client.FullName,
            client.Email,
            clientDebts.GetValueOrDefault(client.Id));

    private static string? Normalize(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
