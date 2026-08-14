using ABP.Application.Common;
using ABP.Application.Features.HermesPay;
using ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.HermesPay;

public sealed class GetHermesTransactionsQueryTests
{
    [Fact]
    public void Validator_allows_route_id_to_be_ignored_and_rejects_invalid_pagination()
    {
        var validator = new GetHermesTransactionsQueryValidator();

        var result = validator.Validate(
            new GetHermesTransactionsQuery(Guid.Empty, 0, 21));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName == nameof(GetHermesTransactionsQuery.RequestedCommerceId));
    }

    [Fact]
    public async Task Handler_uses_resolved_commerce_and_returns_safe_spanish_page()
    {
        var requestedCommerceId = Guid.NewGuid();
        var resolvedCommerceId = Guid.NewGuid();
        var resolver = new FakeCommerceAuthorizationResolverService
        {
            DefaultResult = OperationResult<Guid>.Success(resolvedCommerceId)
        };
        var commerceRepository = new CommerceAuthorizationResolverServiceTests.CommerceRepositoryStub
        {
            Detail = CommerceAuthorizationResolverServiceTests.CreateCommerce(resolvedCommerceId)
        };
        var transactionId = Guid.NewGuid();
        var transactionRepository = new HermesTransactionRepositoryStub
        {
            Page = new PagedResult<HermesTransactionReadModel>(
                [
                    new HermesTransactionReadModel(
                        transactionId,
                        new DateTimeOffset(2026, 8, 12, 14, 30, 0, TimeSpan.Zero),
                        689.25m,
                        "7598",
                        ConsumptionStatus.Approved)
                ],
                2,
                10,
                11)
        };
        var handler = new GetHermesTransactionsQueryHandler(
            resolver,
            commerceRepository,
            transactionRepository);

        var result = await handler.Handle(
            new GetHermesTransactionsQuery(requestedCommerceId, 2, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(resolvedCommerceId, result.Value.CommerceId);
        Assert.Equal("Tienda Hermes", result.Value.CommerceName);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(2, result.Value.TotalPages);
        var transaction = Assert.Single(result.Value.Data);
        Assert.Equal(transactionId, transaction.Id);
        Assert.Equal("7598", transaction.CardLastFourDigits);
        Assert.Equal("APROBADO", transaction.Status);
        Assert.Equal(resolvedCommerceId, transactionRepository.ReceivedCommerceId);
    }

    [Fact]
    public async Task Handler_does_not_read_transactions_when_authorization_fails()
    {
        var resolver = new FakeCommerceAuthorizationResolverService
        {
            DefaultResult = OperationResult<Guid>.Failure(
                HermesPayErrors.CommerceUserInactive)
        };
        var transactionRepository = new HermesTransactionRepositoryStub();
        var handler = new GetHermesTransactionsQueryHandler(
            resolver,
            new CommerceAuthorizationResolverServiceTests.CommerceRepositoryStub(),
            transactionRepository);

        var result = await handler.Handle(
            new GetHermesTransactionsQuery(Guid.NewGuid(), 1, 20),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(HermesPayErrors.CommerceUserInactive, result.Error);
        Assert.Null(transactionRepository.ReceivedCommerceId);
    }

    private sealed class HermesTransactionRepositoryStub : IHermesTransactionRepository
    {
        public PagedResult<HermesTransactionReadModel> Page { get; init; } =
            new([], 1, 20, 0);
        public Guid? ReceivedCommerceId { get; private set; }

        public Task<PagedResult<HermesTransactionReadModel>> GetByCommerceAsync(
            Guid commerceId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ReceivedCommerceId = commerceId;
            return Task.FromResult(Page);
        }
    }
}
