using System.Data;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Implementations;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

public sealed class CreditCardCoreServicesTests
{
    #region Administrative read service tests

    [Fact]
    public async Task List_without_filters_returns_no_search_and_maps_safe_summary()
    {
        var repository = new FakeCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(new CreditCardListRequest());

        Assert.Equal(CreditCardSearchStatus.NoSearch, result.SearchStatus);
        Assert.Single(result.Page.Data);
        Assert.Equal("************1234", result.Page.Data.Single().MaskedCardNumber);
        Assert.Equal("1234", result.Page.Data.Single().LastFourDigits);
        Assert.Equal("08/29", result.Page.Data.Single().ExpirationDate);
        Assert.Equal("Activa", result.Page.Data.Single().Status);
    }

    [Fact]
    public async Task List_with_unknown_identification_returns_client_not_found()
    {
        var repository = new FakeCreditCardRepository();
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Identification: " 999 "));

        Assert.Equal(CreditCardSearchStatus.ClientNotFound, result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
        Assert.Equal("999", repository.ReceivedIdentification);
    }

    [Fact]
    public async Task List_with_existing_client_without_cards_returns_client_without_cards()
    {
        var repository = new FakeCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = false
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Identification: "123"));

        Assert.Equal(CreditCardSearchStatus.ClientWithoutCards, result.SearchStatus);
        Assert.Empty(result.Page.Data);
        Assert.False(repository.SearchWasCalled);
    }

    [Fact]
    public async Task List_with_existing_client_and_no_matching_status_returns_no_matching_cards()
    {
        var repository = new FakeCreditCardRepository
        {
            ClientIdByIdentification = "client-1",
            HasCards = true,
            Page = CreatePage()
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(
                Identification: "123",
                Status: CreditCardStatusFilter.Cancelled));

        Assert.Equal(CreditCardSearchStatus.NoMatchingCards, result.SearchStatus);
        Assert.Empty(result.Page.Data);
    }

    [Fact]
    public async Task List_with_matching_filter_returns_results_found()
    {
        var repository = new FakeCreditCardRepository
        {
            Page = CreatePage(CreateSummary())
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            new CreditCardListRequest(Status: CreditCardStatusFilter.All));

        Assert.Equal(CreditCardSearchStatus.ResultsFound, result.SearchStatus);
        Assert.Equal(1, result.Page.TotalRecords);
    }

    [Fact]
    public async Task GetDetail_maps_consumptions_without_sensitive_fields()
    {
        var cardId = Guid.NewGuid();
        var repository = new FakeCreditCardRepository
        {
            Detail = new CreditCardDetailReadModel(
                cardId,
                "************1234",
                "1234",
                "client-1",
                "María Gómez",
                500m,
                350m,
                150m,
                new DateOnly(2029, 8, 31),
                CreditCardStatus.Active,
                new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero),
                [new CardConsumptionReadModel(
                    Guid.NewGuid(),
                    new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero),
                    25m,
                    "AVANCE",
                    ConsumptionStatus.Approved)])
        };
        var service = CreateService(repository);

        var result = await service.GetDetailAsync(cardId);

        Assert.NotNull(result);
        Assert.Equal("************1234", result.MaskedCardNumber);
        Assert.Equal("08/29", result.ExpirationDate);
        Assert.Single(result.Consumptions);
        Assert.Equal("APROBADO", result.Consumptions.Single().Status);
    }

    [Fact]
    public async Task Client_detail_filters_by_authenticated_client_id()
    {
        var cardId = Guid.NewGuid();
        var repository = new FakeCreditCardRepository
        {
            Detail = CreateDetail(cardId, "client-1")
        };
        var currentUser = new FakeCurrentUserService
        {
            UserId = "client-1",
            Roles = [Roles.Client.ToString()]
        };
        var service = CreateService(
            repository,
            currentUser: currentUser);

        var result = await service.GetClientDetailAsync(cardId);

        Assert.NotNull(result);
        Assert.Equal("client-1", repository.ReceivedDetailClientId);
        Assert.Equal("************1234", result.MaskedCardNumber);
    }

    [Fact]
    public async Task Client_detail_returns_null_for_another_clients_card()
    {
        var repository = new FakeCreditCardRepository
        {
            Detail = CreateDetail(Guid.NewGuid(), "client-1")
        };
        var currentUser = new FakeCurrentUserService
        {
            UserId = "client-2",
            Roles = [Roles.Client.ToString()]
        };
        var service = CreateService(
            repository,
            currentUser: currentUser);

        var result = await service.GetClientDetailAsync(repository.Detail.Id);

        Assert.Null(result);
        Assert.Equal("client-2", repository.ReceivedDetailClientId);
    }

    [Fact]
    public async Task Client_portfolio_returns_safe_active_card_projection()
    {
        var activeCard = new CreditCard
        {
            ClientId = "client-1",
            CardNumber = "4000000000001234",
            Limit = 1_000m,
            Debt = 200m,
            ExpirationDate = new DateOnly(2029, 8, 31),
            Status = CreditCardStatus.Active
        };
        var repository = new FakeCreditCardRepository
        {
            ActiveCards = [activeCard]
        };
        var service = CreateService(
            repository,
            currentUser: new FakeCurrentUserService
            {
                UserId = "client-1",
                Roles = [Roles.Client.ToString()]
            });

        var cards = await service.GetClientActiveCardsAsync();

        var card = Assert.Single(cards);
        Assert.Equal(activeCard.Id, card.Id);
        Assert.Equal("************1234", card.MaskedCardNumber);
        Assert.Equal("08/29", card.ExpirationDate);
        Assert.DoesNotContain("4000000000001234", card.ToString());
    }

    [Fact]
    public async Task List_rejects_invalid_request_through_shared_validator()
    {
        var service = CreateService(new FakeCreditCardRepository());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ListAsync(new CreditCardListRequest(PageSize: 21)));
    }

    [Fact]
    public void Credit_card_profile_is_valid()
    {
        var provider = CreateProvider(new FakeCreditCardRepository());
        var mapper = provider.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    #endregion

    #region Create card tests

    [Fact]
    public async Task Create_assigns_an_active_card_with_safe_generated_values_and_commits_once()
    {
        var repository = new FakeCreditCardRepository
        {
            IsActiveClient = true,
            CardNumberExists = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateOnly(2026, 8, 8));
        var currentUser = new FakeCurrentUserService { UserId = "admin-1" };
        var numberGenerator = new FakeCardNumberGeneratorService("0000000000001234");
        var cvcService = new FakeCvcService { GeneratedCvc = "007" };
        var transaction = new FakeFinancialTransaction();
        var emails = new RecordingCardEmailService
        {
            IsOperationCommitted = () => transaction.IsCommitted
        };
        var users = CreateUsers();
        users.Users["client-1"].Name = "Ana <script>";
        var service = CreateService(
            repository,
            unitOfWork,
            clock,
            currentUser,
            numberGenerator,
            cvcService,
            transaction,
            users,
            emails);

        var operationId = Guid.NewGuid();
        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, operationId));

        Assert.True(result.IsSuccess);
        var card = Assert.IsType<CreditCard>(repository.AddedCard);
        Assert.Equal(card.Id, result.Value);
        Assert.Equal("client-1", card.ClientId);
        Assert.Equal("0000000000001234", card.CardNumber);
        Assert.Equal(cvcService.HashedCvc, card.CvcHash);
        Assert.Equal("007", cvcService.LastHashedCvc);
        Assert.NotEqual(cvcService.GeneratedCvc, card.CvcHash);
        Assert.Equal(5_000m, card.Limit);
        Assert.Equal(0m, card.Debt);
        Assert.Equal(new DateOnly(2029, 8, 31), card.ExpirationDate);
        Assert.Equal(CreditCardStatus.Active, card.Status);
        Assert.Equal("admin-1", card.AssignedByUserId);
        Assert.Equal(operationId, card.CreationOperationId);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
        Assert.False(result.HasNotificationWarning);
        var email = Assert.Single(emails.SentEmails);
        Assert.False(emails.WasCalledBeforeCommit);
        Assert.Contains("1234", email.Body);
        Assert.Contains("5,000.00", email.Body);
        Assert.Contains("08/29", email.Body);
        Assert.Contains("08/08/2026", email.Body);
        Assert.Contains("&lt;script&gt;", email.Body);
        Assert.DoesNotContain("<script>", email.Body);
        Assert.DoesNotContain(card.CardNumber, email.Subject + email.Body);
        Assert.DoesNotContain(cvcService.GeneratedCvc, email.Subject + email.Body);
        Assert.DoesNotContain(cvcService.HashedCvc, email.Subject + email.Body);
    }

    [Fact]
    public async Task Create_exact_replay_returns_existing_id_without_generating_saving_or_emailing()
    {
        var operationId = Guid.NewGuid();
        var existingCard = new CreditCard
        {
            ClientId = "client-1",
            AssignedByUserId = "admin-1",
            Limit = 5_000m,
            CreationOperationId = operationId
        };
        var repository = new FakeCreditCardRepository { ExistingCard = existingCard };
        var unitOfWork = new FakeUnitOfWork();
        var generator = new FakeCardNumberGeneratorService();
        var emails = new RecordingCardEmailService();
        var service = CreateService(
            repository,
            unitOfWork,
            currentUser: new FakeCurrentUserService { UserId = "admin-1" },
            numberGenerator: generator,
            emails: emails);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, operationId));

        Assert.True(result.IsSuccess);
        Assert.Equal(existingCard.Id, result.Value);
        Assert.False(result.HasNotificationWarning);
        Assert.Equal(0, generator.GenerateCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
        Assert.Empty(emails.SentEmails);
    }

    [Fact]
    public async Task Create_reused_operation_with_different_limit_returns_creation_conflict()
    {
        var operationId = Guid.NewGuid();
        var repository = new FakeCreditCardRepository
        {
            ExistingCard = new CreditCard
            {
                ClientId = "client-1",
                AssignedByUserId = "admin-1",
                Limit = 4_000m,
                CreationOperationId = operationId
            }
        };
        var service = CreateService(
            repository,
            currentUser: new FakeCurrentUserService { UserId = "admin-1" });

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, operationId));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.CreationOperationConflict, result.Error);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Create_email_failure_keeps_card_and_returns_notification_warning()
    {
        var repository = new FakeCreditCardRepository
        {
            IsActiveClient = true,
            CardNumberExists = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var emails = new RecordingCardEmailService { ThrowOnSend = true };
        var service = CreateService(
            repository,
            unitOfWork,
            emails: emails);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.True(result.HasNotificationWarning);
        Assert.NotNull(repository.AddedCard);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(1, emails.SendAttempts);
    }

    [Fact]
    public async Task Create_rejects_an_inactive_client_without_generating_or_committing()
    {
        var repository = new FakeCreditCardRepository { IsActiveClient = false };
        var unitOfWork = new FakeUnitOfWork();
        var numberGenerator = new FakeCardNumberGeneratorService();
        var service = CreateService(
            repository,
            unitOfWork,
            numberGenerator: numberGenerator);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.ClientInactive, result.Error);
        Assert.Equal(0, numberGenerator.GenerateCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Create_distinguishes_missing_client_from_inactive_client()
    {
        var repository = new FakeCreditCardRepository
        {
            ClientExists = false,
            IsActiveClient = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("missing-client", 5_000m, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.ClientNotFound, result.Error);
        Assert.Equal(0, repository.IsActiveClientCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Create_requires_an_authenticated_administrator()
    {
        var repository = new FakeCreditCardRepository { IsActiveClient = true };
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUserService
        {
            IsAuthenticated = true,
            UserId = "client-1",
            Roles = [Roles.Client.ToString()]
        };
        var service = CreateService(
            repository,
            unitOfWork,
            currentUser: currentUser);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.AdministratorRequired, result.Error);
        Assert.Equal(0, repository.IsActiveClientCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Create_returns_generation_failure_after_ten_collisions()
    {
        var repository = new FakeCreditCardRepository
        {
            IsActiveClient = true,
            CardNumberExists = true
        };
        var unitOfWork = new FakeUnitOfWork();
        var numberGenerator = new FakeCardNumberGeneratorService();
        var service = CreateService(
            repository,
            unitOfWork,
            numberGenerator: numberGenerator);

        var result = await service.CreateAsync(
            new CreateCreditCardRequest("client-1", 5_000m, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NumberGenerationFailed, result.Error);
        Assert.Equal(10, numberGenerator.GenerateCalls);
        Assert.Equal(10, repository.CardNumberExistsCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    #endregion

    #region Lifecycle service tests

    [Fact]
    public async Task UpdateLimit_updates_active_card_and_commits_once()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 150m, limit: 500m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var emails = new RecordingCardEmailService
        {
            IsOperationCommitted = () => unitOfWork.SaveCalls == 1
        };
        var service = CreateService(repository, unitOfWork, emails: emails);

        var result = await service.UpdateLimitAsync(
            new UpdateCreditLimitRequest(Guid.NewGuid(), 750m));

        Assert.True(result.IsSuccess);
        Assert.Equal(750m, card.Limit);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.False(result.HasNotificationWarning);
        var email = Assert.Single(emails.SentEmails);
        Assert.False(emails.WasCalledBeforeCommit);
        Assert.Contains("1234", email.Body);
        Assert.Contains("750.00", email.Body);
        Assert.Contains("08/08/2026", email.Body);
        Assert.DoesNotContain(card.CardNumber, email.Subject + email.Body);
        Assert.DoesNotContain(card.CvcHash, email.Subject + email.Body);
    }

    [Fact]
    public async Task UpdateLimit_email_failure_keeps_new_limit_and_returns_warning()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 150m, limit: 500m);
        var unitOfWork = new FakeUnitOfWork();
        var emails = new RecordingCardEmailService { ThrowOnSend = true };
        var service = CreateService(
            new FakeCreditCardRepository { CardForUpdate = card },
            unitOfWork,
            emails: emails);

        var result = await service.UpdateLimitAsync(
            new UpdateCreditLimitRequest(Guid.NewGuid(), 750m));

        Assert.True(result.IsSuccess);
        Assert.True(result.HasNotificationWarning);
        Assert.Equal(750m, card.Limit);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(1, emails.SendAttempts);
    }

    [Fact]
    public async Task UpdateLimit_rejects_limit_below_debt_without_committing()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 500m, limit: 700m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.UpdateLimitAsync(
            new UpdateCreditLimitRequest(Guid.NewGuid(), 499m));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.LimitBelowDebt, result.Error);
        Assert.Equal(700m, card.Limit);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task UpdateLimit_rejects_cancelled_card_without_committing()
    {
        var card = CreateCard(CreditCardStatus.Cancelled, debt: 0m, limit: 500m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.UpdateLimitAsync(
            new UpdateCreditLimitRequest(Guid.NewGuid(), 750m));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.Cancelled, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task UpdateLimit_returns_not_found_without_committing()
    {
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCreditCardRepository(), unitOfWork);

        var result = await service.UpdateLimitAsync(
            new UpdateCreditLimitRequest(Guid.NewGuid(), 750m));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Cancel_changes_active_debt_free_card_and_commits_once()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 0m, limit: 500m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.CancelAsync(new CancelCreditCardRequest(Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal(CreditCardStatus.Cancelled, card.Status);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Cancel_rejects_card_with_debt_without_committing()
    {
        var card = CreateCard(CreditCardStatus.Active, debt: 0.01m, limit: 500m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.CancelAsync(new CancelCreditCardRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.OutstandingDebt, result.Error);
        Assert.Equal(CreditCardStatus.Active, card.Status);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Cancel_rejects_already_cancelled_card_without_committing()
    {
        var card = CreateCard(CreditCardStatus.Cancelled, debt: 0m, limit: 500m);
        var repository = new FakeCreditCardRepository { CardForUpdate = card };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        var result = await service.CancelAsync(new CancelCreditCardRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.Cancelled, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Cancel_returns_not_found_without_committing()
    {
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCreditCardRepository(), unitOfWork);

        var result = await service.CancelAsync(
            new CancelCreditCardRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CreditCardErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    #endregion

    #region Supporting credit card service tests

    [Fact]
    public void Card_number_generator_returns_exactly_sixteen_digits()
    {
        var generator = new CardNumberGeneratorService();

        var cardNumber = generator.Generate();

        Assert.Equal(16, cardNumber.Length);
        Assert.All(cardNumber, character => Assert.InRange(character, '0', '9'));
    }

    #endregion

    #region Test helpers

    private static ICreditCardService CreateService(
        FakeCreditCardRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        IClock? clock = null,
        ICurrentUserService? currentUser = null,
        ICardNumberGeneratorService? numberGenerator = null,
        ICvcService? cvcService = null,
        IFinancialTransaction? financialTransaction = null,
        StubCardUserRepository? users = null,
        RecordingCardEmailService? emails = null) =>
        CreateProvider(
            repository,
            unitOfWork,
            clock,
            currentUser,
            numberGenerator,
            cvcService,
            financialTransaction,
            users,
            emails).GetRequiredService<ICreditCardService>();

    private static IServiceProvider CreateProvider(
        FakeCreditCardRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        IClock? clock = null,
        ICurrentUserService? currentUser = null,
        ICardNumberGeneratorService? numberGenerator = null,
        ICvcService? cvcService = null,
        IFinancialTransaction? financialTransaction = null,
        StubCardUserRepository? users = null,
        RecordingCardEmailService? emails = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<ILogger<CreditCardService>>(
            NullLogger<CreditCardService>.Instance);
        services.AddApplicationServices();
        services.AddSingleton<ICreditCardRepository>(repository);
        services.AddSingleton<IUnitOfWork>(unitOfWork ?? new FakeUnitOfWork());
        services.AddSingleton(
            financialTransaction ?? new FakeFinancialTransaction());
        services.AddSingleton(clock ?? new FakeClock(new DateOnly(2026, 8, 8)));
        services.AddSingleton(
            currentUser ?? new FakeCurrentUserService());
        services.AddSingleton<ICardNumberGeneratorService>(
            numberGenerator ?? new FakeCardNumberGeneratorService());
        services.AddSingleton<ICvcService>(
            cvcService ?? new FakeCvcService());
        services.AddSingleton<IUserRepository>(users ?? CreateUsers());
        services.AddSingleton<IEmailService>(
            emails ?? new RecordingCardEmailService());
        return services.BuildServiceProvider();
    }

    private static StubCardUserRepository CreateUsers()
    {
        var users = new StubCardUserRepository();
        users.Users["client-1"] = new User("client-1")
        {
            Name = "Ana",
            LastName = "Pérez",
            Email = "client@example.com",
            Role = Roles.Client,
            IsActive = true
        };
        return users;
    }

    private sealed class FakeFinancialTransaction : IFinancialTransaction
    {
        public IsolationLevel? IsolationLevel { get; private set; }

        public bool IsCommitted { get; private set; }

        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            IsCommitted = true;
            return result;
        }

        public async Task<TResult> ExecuteAsync<TResult>(
            IsolationLevel isolationLevel,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            IsolationLevel = isolationLevel;
            var result = await operation(cancellationToken);
            IsCommitted = true;
            return result;
        }
    }

    private static CreditCard CreateCard(
        CreditCardStatus status,
        decimal debt,
        decimal limit) =>
        new()
        {
            ClientId = "client-1",
            CardNumber = "4111111111111234",
            CvcHash = "hashed-cvc",
            Status = status,
            Debt = debt,
            Limit = limit
        };

    private static PagedResult<CreditCardSummaryReadModel> CreatePage(
        params CreditCardSummaryReadModel[] data) =>
        new(data, 1, 20, data.Length);

    private static CreditCardSummaryReadModel CreateSummary() =>
        new(
            Guid.NewGuid(),
            "************1234",
            "1234",
            "client-1",
            "María Gómez",
            500m,
            350m,
            150m,
            new DateOnly(2029, 8, 31),
            CreditCardStatus.Active,
            new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero));

    private static CreditCardDetailReadModel CreateDetail(
        Guid cardId,
        string clientId) =>
        new(
            cardId,
            "************1234",
            "1234",
            clientId,
            "María Gómez",
            500m,
            350m,
            150m,
            new DateOnly(2029, 8, 31),
            CreditCardStatus.Active,
            new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero),
            Array.Empty<CardConsumptionReadModel>());

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        public bool ClientExists { get; init; } = true;

        public string? ClientIdByIdentification { get; init; }

        public bool HasCards { get; init; } = true;

        public PagedResult<CreditCardSummaryReadModel> Page { get; init; } = CreatePage();

        public CreditCardDetailReadModel? Detail { get; init; }

        public string? ReceivedDetailClientId { get; private set; }

        public decimal ActiveDebt { get; init; }

        public IReadOnlyCollection<CreditCard> ActiveCards { get; init; } =
            Array.Empty<CreditCard>();

        public bool SearchWasCalled { get; private set; }

        public string? ReceivedIdentification { get; private set; }
        public bool IsActiveClient { get; init; }
        public CreditCard? CardForUpdate { get; init; }
        public bool CardNumberExists { get; init; }
        public int CardNumberExistsCalls { get; private set; }
        public int IsActiveClientCalls { get; private set; }
        public int AddCalls { get; private set; }
        public CreditCard? AddedCard { get; private set; }

        public CreditCard? ExistingCard { get; init; }

        public Task<CreditCard?> GetByCreationOperationIdAsync(
            Guid operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                ExistingCard?.CreationOperationId == operationId
                    ? ExistingCard
                    : null);

        public Task<bool> ClientExistsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ClientExists);

        public Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> CardNumberExistsAsync(
            string cardNumber,
            CancellationToken cancellationToken = default)
        {
            CardNumberExistsCalls++;
            return Task.FromResult(CardNumberExists);
        }

        public Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CardPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => Task.FromResult<CardPayment?>(null);
        public Task<CardConsumption?> GetConsumptionByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => Task.FromResult<CardConsumption?>(null);
        public Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => Task.FromResult(ActiveCards);

        public Task<string?> FindClientIdByIdentificationAsync(
            string identification,
            CancellationToken cancellationToken = default)
        {
            ReceivedIdentification = identification;
            return Task.FromResult(ClientIdByIdentification);
        }

        public Task<bool> HasAnyCardsAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HasCards);

        public Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
            int page,
            int pageSize,
            string? identification = null,
            CreditCardStatusFilter? status = null,
            CancellationToken cancellationToken = default)
        {
            SearchWasCalled = true;
            return Task.FromResult(Page);
        }

        public Task<CreditCardDetailReadModel?> GetDetailsAsync(
            Guid creditCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
            Guid creditCardId,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            ReceivedDetailClientId = clientId;
            return Task.FromResult(
                Detail?.ClientId == clientId ? Detail : null);
        }

        public Task<decimal> GetActiveDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveDebt);

        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveDebt);

        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
            IReadOnlyCollection<string> clientIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, decimal>>(
                clientIds.ToDictionary(clientId => clientId, _ => ActiveDebt));

        public Task<CreditCard> AddAsync(
            CreditCard entity,
            CancellationToken cancellationToken = default)
        {
            AddCalls++;
            AddedCard = entity;
            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<CreditCard>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IQueryable<CreditCard> GetAllQueryable(bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<CreditCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> UpdateAsync(Guid id, CreditCard value, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CreditCard?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> IsActiveClientAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            IsActiveClientCalls++;
            return Task.FromResult(IsActiveClient);
        }

        public Task<CreditCard?> GetForUpdateAsync(Guid creditCardId, CancellationToken cancellationToken = default) => Task.FromResult(CardForUpdate);

    }

    #endregion
}
