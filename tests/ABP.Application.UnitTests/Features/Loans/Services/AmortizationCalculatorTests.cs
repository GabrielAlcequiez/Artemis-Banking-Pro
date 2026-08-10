using ABP.Application.Features.Loans.Services.Implementations;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class AmortizationCalculatorTests
{
    private readonly AmortizationCalculator _calculator = new();

    [Fact]
    public void Calculate_with_known_rate_generates_french_amortization_schedule()
    {
        var result = _calculator.Calculate(
            100_000m,
            12m,
            12,
            new DateOnly(2026, 7, 1));

        var installments = result.Installments.ToArray();

        Assert.Equal(8_884.88m, result.MonthlyInstallment);
        Assert.Equal(12, installments.Length);
        Assert.Equal(1_000m, installments[0].InterestAmount);
        Assert.Equal(7_884.88m, installments[0].CapitalAmount);
        Assert.Equal(921.15m, installments[1].InterestAmount);
        Assert.Equal(7_963.73m, installments[1].CapitalAmount);
        Assert.Equal(
            installments.Sum(x => x.InstallmentAmount),
            result.TotalAmountToPay);
    }

    [Fact]
    public void Calculate_with_zero_rate_distributes_only_capital()
    {
        var result = _calculator.Calculate(
            1_000m,
            0m,
            6,
            new DateOnly(2026, 1, 15));

        var installments = result.Installments.ToArray();

        Assert.Equal(166.67m, result.MonthlyInstallment);
        Assert.Equal(0m, installments.Sum(x => x.InterestAmount));
        Assert.Equal(1_000m, installments.Sum(x => x.CapitalAmount));
        Assert.Equal(166.65m, installments[^1].InstallmentAmount);
        Assert.Equal(1_000m, result.TotalAmountToPay);
    }

    [Fact]
    public void Calculate_adjusts_last_installment_and_initializes_pending_state()
    {
        const decimal capital = 10_000m;

        var result = _calculator.Calculate(
            capital,
            10.5m,
            18,
            new DateOnly(2026, 8, 8));

        Assert.Equal(capital, result.Installments.Sum(x => x.CapitalAmount));
        Assert.All(result.Installments, installment =>
        {
            Assert.Equal(
                installment.InstallmentAmount,
                installment.InterestAmount + installment.CapitalAmount);
            Assert.Equal(installment.InstallmentAmount, installment.PendingInstallmentAmount);
            Assert.Equal("Pendiente", installment.PaymentStatus);
            Assert.False(installment.IsLate);
        });
    }

    [Theory]
    [InlineData(2025, 1, 31, 2025, 2, 28, 2025, 3, 31, 2025, 4, 30)]
    [InlineData(2024, 1, 30, 2024, 2, 29, 2024, 3, 30, 2024, 4, 30)]
    [InlineData(2025, 11, 30, 2025, 12, 30, 2026, 1, 30, 2026, 2, 28)]
    public void Calculate_preserves_original_due_day_when_destination_month_allows_it(
        int creationYear,
        int creationMonth,
        int creationDay,
        int firstYear,
        int firstMonth,
        int firstDay,
        int secondYear,
        int secondMonth,
        int secondDay,
        int thirdYear,
        int thirdMonth,
        int thirdDay)
    {
        var result = _calculator.Calculate(
            600m,
            0m,
            3,
            new DateOnly(creationYear, creationMonth, creationDay));

        var dueDates = result.Installments.Select(x => x.DueDate).ToArray();

        Assert.Equal(new DateOnly(firstYear, firstMonth, firstDay), dueDates[0]);
        Assert.Equal(new DateOnly(secondYear, secondMonth, secondDay), dueDates[1]);
        Assert.Equal(new DateOnly(thirdYear, thirdMonth, thirdDay), dueDates[2]);
    }

    [Fact]
    public void Calculate_rejects_non_positive_capital()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(0m, 12m, 12, new DateOnly(2026, 8, 8)));
    }

    [Fact]
    public void Calculate_rejects_negative_interest_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(10_000m, -0.01m, 12, new DateOnly(2026, 8, 8)));
    }

    [Fact]
    public void Calculate_rejects_non_positive_term()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(10_000m, 12m, 0, new DateOnly(2026, 8, 8)));
    }
}
