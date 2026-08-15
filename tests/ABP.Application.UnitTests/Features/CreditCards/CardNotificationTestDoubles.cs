using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.UnitTests.Features.CreditCards;

internal sealed class RecordingCardEmailService : IEmailService
{
    public List<EmailRequestDto> SentEmails { get; } = [];

    public int SendAttempts { get; private set; }

    public bool ThrowOnSend { get; init; }

    public Func<bool>? IsOperationCommitted { get; init; }

    public bool WasCalledBeforeCommit { get; private set; }

    public Task SendAsync(EmailRequestDto emailRequestDto)
    {
        SendAttempts++;
        WasCalledBeforeCommit |= IsOperationCommitted is not null &&
                                 !IsOperationCommitted();

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Fallo SMTP simulado.");
        }

        SentEmails.Add(emailRequestDto);
        return Task.CompletedTask;
    }
}

internal sealed class StubCardUserRepository : IUserRepository
{
    public Dictionary<string, User> Users { get; } = [];

    public Task<User?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.GetValueOrDefault(id));

    public Task<User?> GetActiveClientByIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        GetByIdAsync(clientId, cancellationToken);

    public Task<PagedResult<User>> GetActiveClientsPagedAsync(
        PagedRequest request,
        string? identification = null,
        CancellationToken cancellationToken = default)
    {
        var clients = Users.Values
            .Where(user => user.Role == Roles.Client && user.IsActive)
            .ToArray();
        return Task.FromResult(
            new PagedResult<User>(clients, request.Page, request.PageSize, clients.Length));
    }

    public Task<int> CountActiveClientsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Users.Values.Count(user => user.Role == Roles.Client && user.IsActive));

    public Task<bool> ExistsByCommerceIdAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<User?> FindByIdentificationAsync(string identification) =>
        Task.FromResult(
            Users.Values.FirstOrDefault(user => user.Identification == identification));

    public Task<PagedResult<User>> GetPagedAsync(
        PagedRequest request,
        bool commerceOnly = false,
        Roles? role = null,
        CancellationToken cancellationToken = default)
    {
        var users = Users.Values
            .Where(user => !role.HasValue || user.Role == role)
            .ToArray();
        return Task.FromResult(
            new PagedResult<User>(users, request.Page, request.PageSize, users.Length));
    }

    public IQueryable<User> GetAllQueryable(bool trackChanges = false) =>
        Users.Values.AsQueryable();

    public Task<IReadOnlyList<User>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.Values.ToArray());

    public Task<User> AddAsync(
        User entity,
        CancellationToken cancellationToken = default)
    {
        Users[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<User?> UpdateAsync(
        string id,
        User value,
        CancellationToken cancellationToken = default)
    {
        Users[id] = value;
        return Task.FromResult<User?>(value);
    }

    public Task<User?> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);
}
