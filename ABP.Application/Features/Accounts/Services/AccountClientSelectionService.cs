using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Interfaces;
using FluentValidation;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Looks up active clients for the Admin "open secondary account" flow.</summary>
    public sealed class AccountClientSelectionService(
        IUserRepository userRepository,
        IValidator<AccountClientSearchRequest> requestValidator) : IAccountClientSelectionService
    {
        public async Task<PagedResult<AccountClientCandidateDto>> SearchAsync(
            AccountClientSearchRequest request, CancellationToken cancellationToken = default)
        {
            await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var normalizedRequest = request with { Identification = Normalize(request.Identification) };

            var clients = await userRepository.GetActiveClientsPagedAsync(
                new PagedRequest(normalizedRequest.Page, normalizedRequest.PageSize),
                normalizedRequest.Identification,
                cancellationToken);

            var candidates = clients.Data.Select(Map).ToArray();

            return new PagedResult<AccountClientCandidateDto>(
                candidates, clients.Page, clients.PageSize, clients.TotalRecords);
        }

        public async Task<AccountClientCandidateDto?> GetActiveClientAsync(
            string clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            var client = await userRepository.GetActiveClientByIdAsync(clientId, cancellationToken);

            return client is null ? null : Map(client);
        }

        private static AccountClientCandidateDto Map(User client) =>
            new(client.Id, client.Identification, $"{client.Name} {client.LastName}".Trim(), client.Email);

        private static string? Normalize(string? identification) =>
            string.IsNullOrWhiteSpace(identification) ? null : identification.Trim();
    }
}
