using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;
using FluentValidation;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanRiskServiceTests
{
    [Fact]
    public async Task Assess_returns_current_high_risk_and_uses_amortization_total()
    {
        var bankingDate = new DateOnly(2026, 8, 15);
        var debts = new FakeCustomerDebtService
        {
            CurrentDebt = 120m,
            AverageDebt = 100m
        };
        var calculator = new FakeAmortizationCalculator
        {
            Result = new AmortizationResult(10m, 50m, [])
        };
        var service = CreateService(debts, calculator, bankingDate);
        var request = CreateRequest();

        var result = await service.AssessAsync(request);

        Assert.Equal("CurrentHighRisk", result.RiskType);
        Assert.Equal(120m, result.CurrentDebt);
        Assert.Equal(170m, result.ProjectedDebt);
        Assert.Equal(100m, result.AverageDebt);
        Assert.True(result.RequiresConfirmation);
        Assert.Equal(request.CapitalAmount, calculator.ReceivedCapital);
        Assert.Equal(request.AnnualInterestRate, calculator.ReceivedAnnualInterestRate);
        Assert.Equal(request.TermInMonths, calculator.ReceivedTermInMonths);
        Assert.Equal(bankingDate, calculator.ReceivedCreationDate);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Projected_high_risk_requires_confirmation_only_when_not_confirmed(
        bool confirmHighRisk,
        bool expectedRequiresConfirmation)
    {
        var service = CreateService(
            new FakeCustomerDebtService
            {
                CurrentDebt = 40m,
                AverageDebt = 100m
            },
            new FakeAmortizationCalculator
            {
                Result = new AmortizationResult(10m, 70m, [])
            });

        var result = await service.AssessAsync(
            CreateRequest(confirmHighRisk));

        Assert.Equal("ProjectedHighRisk", result.RiskType);
        Assert.Equal(110m, result.ProjectedDebt);
        Assert.Equal(expectedRequiresConfirmation, result.RequiresConfirmation);
    }

    [Fact]
    public async Task Projected_debt_equal_to_average_does_not_require_confirmation()
    {
        var service = CreateService(
            new FakeCustomerDebtService
            {
                CurrentDebt = 20m,
                AverageDebt = 100m
            },
            new FakeAmortizationCalculator
            {
                Result = new AmortizationResult(10m, 80m, [])
            });

        var result = await service.AssessAsync(CreateRequest());

        Assert.Equal("None", result.RiskType);
        Assert.Equal(100m, result.ProjectedDebt);
        Assert.False(result.RequiresConfirmation);
    }

    [Fact]
    public async Task Positive_projected_debt_with_zero_average_is_projected_high_risk()
    {
        var service = CreateService(
            new FakeCustomerDebtService(),
            new FakeAmortizationCalculator
            {
                Result = new AmortizationResult(10m, 50m, [])
            });

        var result = await service.AssessAsync(CreateRequest());

        Assert.Equal("ProjectedHighRisk", result.RiskType);
        Assert.Equal(0m, result.AverageDebt);
        Assert.True(result.RequiresConfirmation);
    }

    [Fact]
    public async Task Invalid_request_is_rejected_before_calculation_or_debt_queries()
    {
        var debts = new FakeCustomerDebtService();
        var calculator = new FakeAmortizationCalculator();
        var service = CreateService(debts, calculator);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AssessAsync(CreateRequest() with { CapitalAmount = 0m }));

        Assert.Equal(0, calculator.CalculateCalls);
        Assert.Equal(0, debts.CurrentDebtCalls);
        Assert.Equal(0, debts.AverageDebtCalls);
    }

    private static LoanRiskService CreateService(
        ICustomerDebtService debts,
        IAmortizationCalculator calculator,
        DateOnly? today = null) =>
        new(
            debts,
            calculator,
            new FakeClock(today ?? new DateOnly(2026, 8, 15)),
            new CreateLoanRequestValidator());

    private static CreateLoanRequest CreateRequest(
        bool confirmHighRisk = false) =>
        new(
            "client-1",
            1_000m,
            12,
            12m,
            confirmHighRisk);

    private sealed class FakeCustomerDebtService : ICustomerDebtService
    {
        public decimal CurrentDebt { get; init; }

        public decimal AverageDebt { get; init; }

        public int CurrentDebtCalls { get; private set; }

        public int AverageDebtCalls { get; private set; }

        public Task<decimal> GetTotalDebtAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            CurrentDebtCalls++;
            return Task.FromResult(CurrentDebt);
        }

        public Task<IReadOnlyDictionary<string, decimal>> GetTotalDebtsAsync(
            IReadOnlyCollection<string> clientIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetAverageActiveClientDebtAsync(
            CancellationToken cancellationToken = default)
        {
            AverageDebtCalls++;
            return Task.FromResult(AverageDebt);
        }
    }

    private sealed class FakeAmortizationCalculator : IAmortizationCalculator
    {
        public AmortizationResult Result { get; init; } = new(0m, 0m, []);

        public int CalculateCalls { get; private set; }

        public decimal ReceivedCapital { get; private set; }

        public decimal ReceivedAnnualInterestRate { get; private set; }

        public int ReceivedTermInMonths { get; private set; }

        public DateOnly ReceivedCreationDate { get; private set; }

        public AmortizationResult Calculate(
            decimal capital,
            decimal annualInterestRate,
            int termInMonths,
            DateOnly creationDate)
        {
            CalculateCalls++;
            ReceivedCapital = capital;
            ReceivedAnnualInterestRate = annualInterestRate;
            ReceivedTermInMonths = termInMonths;
            ReceivedCreationDate = creationDate;
            return Result;
        }
    }

    private sealed class FakeClock(DateOnly today) : IClock
    {
        public DateTimeOffset UtcNow =>
            new(today.Year, today.Month, today.Day, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => today;
    }
}
