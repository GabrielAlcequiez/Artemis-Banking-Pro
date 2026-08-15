using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Services.Implementations;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Commerce;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Application.UnitTests.Features.HermesPay;

public sealed class CommerceAuthorizationResolverServiceTests
{
    [Fact]
    public async Task Administrator_uses_route_commerce_when_commerce_and_user_are_active()
    {
        var commerceId = Guid.NewGuid();
        var commerceRepository = new CommerceRepositoryStub
        {
            Detail = CreateCommerce(commerceId)
        };
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Administrator, "admin-1"),
            new UserRepositoryStub(),
            commerceRepository);

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(commerceId);

        Assert.True(result.IsSuccess);
        Assert.Equal(commerceId, result.Value);
        Assert.Equal(commerceId, commerceRepository.ReceivedCommerceId);
    }

    [Fact]
    public async Task Commerce_ignores_route_and_reloads_current_association_from_database()
    {
        var requestedCommerceId = Guid.NewGuid();
        var persistedCommerceId = Guid.NewGuid();
        var userRepository = new UserRepositoryStub
        {
            User = new User("commerce-user")
            {
                Role = Roles.Commerce,
                IsActive = true,
                CommerceId = persistedCommerceId
            }
        };
        var commerceRepository = new CommerceRepositoryStub
        {
            Detail = CreateCommerce(persistedCommerceId, "commerce-user")
        };
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Commerce, "commerce-user", requestedCommerceId),
            userRepository,
            commerceRepository);

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(requestedCommerceId);

        Assert.True(result.IsSuccess);
        Assert.Equal(persistedCommerceId, result.Value);
        Assert.Equal("commerce-user", userRepository.ReceivedUserId);
        Assert.Equal(persistedCommerceId, commerceRepository.ReceivedCommerceId);
    }

    [Fact]
    public async Task Commerce_with_stale_jwt_is_rejected_when_persisted_user_is_inactive()
    {
        var userRepository = new UserRepositoryStub
        {
            User = new User("commerce-user")
            {
                Role = Roles.Commerce,
                IsActive = false,
                CommerceId = Guid.NewGuid()
            }
        };
        var commerceRepository = new CommerceRepositoryStub();
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Commerce, "commerce-user"),
            userRepository,
            commerceRepository);

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.CommerceUserInactive, result.Error);
        Assert.Null(commerceRepository.ReceivedCommerceId);
    }

    [Fact]
    public async Task Commerce_without_current_database_association_is_forbidden()
    {
        var userRepository = new UserRepositoryStub
        {
            User = new User("commerce-user")
            {
                Role = Roles.Commerce,
                IsActive = true,
                CommerceId = null
            }
        };
        var commerceRepository = new CommerceRepositoryStub();
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Commerce, "commerce-user", Guid.NewGuid()),
            userRepository,
            commerceRepository);

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.CommerceAssociationRequired, result.Error);
        Assert.Null(commerceRepository.ReceivedCommerceId);
    }

    [Fact]
    public async Task Administrator_cannot_query_commerce_without_associated_user()
    {
        var commerceId = Guid.NewGuid();
        var detail = CreateCommerce(commerceId) with { AssociatedUser = null };
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Administrator, "admin-1"),
            new UserRepositoryStub(),
            new CommerceRepositoryStub { Detail = detail });

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(commerceId);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.AssociatedCommerceUserRequired, result.Error);
    }

    [Theory]
    [InlineData(false, true, "HermesPay.CommerceInactive")]
    [InlineData(true, false, "HermesPay.AssociatedCommerceUserInactive")]
    public async Task Resolver_revalidates_commerce_and_associated_user_state(
        bool commerceActive,
        bool associatedUserActive,
        string expectedErrorCode)
    {
        var commerceId = Guid.NewGuid();
        var commerceRepository = new CommerceRepositoryStub
        {
            Detail = CreateCommerce(
                commerceId,
                isActive: commerceActive,
                associatedUserActive: associatedUserActive)
        };
        var resolver = CreateResolver(
            new CurrentUserStub(Roles.Administrator, "admin-1"),
            new UserRepositoryStub(),
            commerceRepository);

        var result = await resolver.ResolveAuthorizedCommerceIdAsync(commerceId);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedErrorCode, result.Error.Code);
    }

    private static CommerceAuthorizationResolverService CreateResolver(
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        ICommerceRepository commerceRepository) =>
        new(currentUser, userRepository, commerceRepository);

    internal static CommerceDetailReadModel CreateCommerce(
        Guid commerceId,
        string userId = "commerce-user",
        bool isActive = true,
        bool associatedUserActive = true) =>
        new(
            commerceId,
            "Tienda Hermes",
            null,
            "hermes@example.test",
            "8095551234",
            "101999999",
            isActive ? CommerceStatus.Active : CommerceStatus.Inactive,
            DateTimeOffset.UtcNow,
            new AssociatedCommerceUserReadModel(
                userId,
                "hermes-commerce",
                "hermes@example.test",
                associatedUserActive));

    internal sealed class CurrentUserStub(
        Roles role,
        string? userId,
        Guid? commerceId = null) : ICurrentUserService
    {
        public bool IsAuthenticated { get; init; } = true;
        public string? UserId { get; } = userId;
        public string? UserName => UserId;
        public Guid? CommerceId { get; } = commerceId;
        public IReadOnlyCollection<string> Roles { get; } = [role.ToString()];
        public bool IsInRole(string requestedRole) => Roles.Contains(requestedRole);
    }

    internal sealed class CommerceRepositoryStub : ICommerceRepository
    {
        public CommerceDetailReadModel? Detail { get; init; }
        public Guid? ReceivedCommerceId { get; private set; }

        public Task<CommerceDetailReadModel?> GetDetailsAsync(Guid commerceId, CancellationToken cancellationToken = default)
        {
            ReceivedCommerceId = commerceId;
            return Task.FromResult(Detail);
        }

        public Task<bool> EmailExistsAsync(string email, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RncExistsAsync(string rnc, Guid? excludingCommerceId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<CommerceSummaryReadModel>> SearchAsync(int page, int pageSize, CommerceStatusFilter? status = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CommerceEntity?> GetForUpdateAsync(Guid commerceId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<CommerceEntity> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<CommerceEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<CommerceEntity>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CommerceEntity> AddAsync(CommerceEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CommerceEntity?> UpdateAsync(Guid id, CommerceEntity value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CommerceEntity?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    internal sealed class UserRepositoryStub : IUserRepository
    {
        public User? User { get; init; }
        public string? ReceivedUserId { get; private set; }

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ReceivedUserId = id;
            return Task.FromResult(User);
        }

        public Task<User?> FindByIdentificationAsync(string identification) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetActiveClientsPagedAsync(PagedRequest request, string? identification = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetActiveClientByIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> UpdateAsync(string id, User value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
