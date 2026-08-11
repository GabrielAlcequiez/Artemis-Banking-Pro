using ABP.Application.Common.Services.Implementations;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CustomerDebtAndClientRepositoryTests
{
    [Fact]
    public async Task User_repository_returns_only_active_clients_and_supports_identification_search()
    {
        await using var context = CreateContext();
        var (firstClient, secondClient, inactiveClient, administrator) =
            CreateUsers();
        context.Users.AddRange(
            secondClient,
            inactiveClient,
            administrator,
            firstClient);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var firstPage = await repository.GetActiveClientsPagedAsync(
            new PagedRequest(1, 1));
        var filtered = await repository.GetActiveClientsPagedAsync(
            new PagedRequest(1, 20),
            " 00200000002 ");

        Assert.Equal(2, firstPage.TotalRecords);
        Assert.Single(firstPage.Data);
        Assert.Equal(firstClient.Id, firstPage.Data.Single().Id);
        Assert.Single(filtered.Data);
        Assert.Equal(secondClient.Id, filtered.Data.Single().Id);
        Assert.Equal(2, await repository.CountActiveClientsAsync());
        Assert.NotNull(await repository.GetActiveClientByIdAsync(firstClient.Id));
        Assert.Null(await repository.GetActiveClientByIdAsync(inactiveClient.Id));
        Assert.Null(await repository.GetActiveClientByIdAsync(administrator.Id));
    }

    [Fact]
    public async Task Customer_debt_service_combines_active_loan_and_card_debt_and_calculates_average()
    {
        await using var context = CreateContext();
        var (firstClient, secondClient, inactiveClient, administrator) =
            CreateUsers();
        context.Users.AddRange(
            firstClient,
            secondClient,
            inactiveClient,
            administrator);

        context.Loans.AddRange(
            CreateLoan(firstClient, administrator, "100000001", 100m, LoanStatus.Active),
            CreateLoan(secondClient, administrator, "100000002", 999m, LoanStatus.Completed),
            CreateLoan(inactiveClient, administrator, "100000003", 1_000m, LoanStatus.Active));
        context.CreditCards.AddRange(
            CreateCard(firstClient.Id, administrator.Id, "4000000000000001", 50m, CreditCardStatus.Active),
            CreateCard(secondClient.Id, administrator.Id, "4000000000000002", 150m, CreditCardStatus.Active),
            CreateCard(secondClient.Id, administrator.Id, "4000000000000003", 999m, CreditCardStatus.Cancelled),
            CreateCard(inactiveClient.Id, administrator.Id, "4000000000000004", 1_000m, CreditCardStatus.Active));
        await context.SaveChangesAsync();

        var service = new CustomerDebtService(
            new UserRepository(context),
            new LoanRepository(context),
            new CreditCardRepository(context));

        var firstClientDebt = await service.GetTotalDebtAsync(firstClient.Id);
        var pageDebts = await service.GetTotalDebtsAsync(
            [firstClient.Id, secondClient.Id]);
        var averageDebt = await service.GetAverageActiveClientDebtAsync();

        Assert.Equal(150m, firstClientDebt);
        Assert.Equal(150m, pageDebts[firstClient.Id]);
        Assert.Equal(150m, pageDebts[secondClient.Id]);
        Assert.Equal(150m, averageDebt);
    }

    [Fact]
    public async Task Customer_debt_service_returns_zero_average_when_there_are_no_active_clients()
    {
        await using var context = CreateContext();
        var service = new CustomerDebtService(
            new UserRepository(context),
            new LoanRepository(context),
            new CreditCardRepository(context));

        var averageDebt = await service.GetAverageActiveClientDebtAsync();

        Assert.Equal(0m, averageDebt);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CustomerDebtTests_{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static (User First, User Second, User Inactive, User Administrator) CreateUsers()
    {
        var first = CreateUser(
            "client-1",
            "00100000001",
            Roles.Client,
            isActive: true);
        var second = CreateUser(
            "client-2",
            "00200000002",
            Roles.Client,
            isActive: true);
        var inactive = CreateUser(
            "client-3",
            "00300000003",
            Roles.Client,
            isActive: false);
        var administrator = CreateUser(
            "admin-1",
            "00400000004",
            Roles.Administrator,
            isActive: true);

        return (first, second, inactive, administrator);
    }

    private static User CreateUser(
        string id,
        string identification,
        Roles role,
        bool isActive) =>
        new(id)
        {
            Name = "Nombre",
            LastName = id,
            Email = $"{id}@example.test",
            UserName = id,
            Identification = identification,
            Role = role,
            IsActive = isActive
        };

    private static Loan CreateLoan(
        User client,
        User administrator,
        string loanNumber,
        decimal pendingAmount,
        LoanStatus status) =>
        new()
        {
            ClientId = client.Id,
            Client = client,
            LoanNumber = loanNumber,
            Capital = Math.Max(pendingAmount, 1m),
            PendingAmount = pendingAmount,
            AnnualInterestRate = 10m,
            TermInMonths = 12,
            Status = status,
            AssignedByUserId = administrator.Id,
            AssignedByUser = administrator
        };

    private static CreditCard CreateCard(
        string clientId,
        string administratorId,
        string cardNumber,
        decimal debt,
        CreditCardStatus status) =>
        new()
        {
            ClientId = clientId,
            AssignedByUserId = administratorId,
            CardNumber = cardNumber,
            CvcHash = new string('a', 64),
            Limit = Math.Max(debt, 1_500m),
            Debt = debt,
            ExpirationDate = new DateOnly(2030, 12, 31),
            Status = status
        };
}
