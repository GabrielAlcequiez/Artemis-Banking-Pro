using ABP.Domain.Enums;
using ABP.Domain.Rules.Loans;

namespace ABP.Domain.UnitTests.Rules.Loans;

public sealed class LoanRiskPolicyTests
{
    [Fact]
    public void Current_debt_above_average_is_current_high_risk()
    {
        var result = LoanRiskPolicy.Evaluate(
            currentDebt: 101m,
            projectedDebt: 150m,
            averageDebt: 100m);

        Assert.Equal(LoanRiskType.CurrentHighRisk, result);
    }

    [Fact]
    public void Projected_debt_above_average_is_projected_high_risk()
    {
        var result = LoanRiskPolicy.Evaluate(
            currentDebt: 75m,
            projectedDebt: 125m,
            averageDebt: 100m);

        Assert.Equal(LoanRiskType.ProjectedHighRisk, result);
    }

    [Fact]
    public void Projected_debt_equal_to_average_is_not_high_risk()
    {
        var result = LoanRiskPolicy.Evaluate(
            currentDebt: 50m,
            projectedDebt: 100m,
            averageDebt: 100m);

        Assert.Equal(LoanRiskType.None, result);
    }

    [Fact]
    public void Positive_projected_debt_with_zero_average_is_projected_high_risk()
    {
        var result = LoanRiskPolicy.Evaluate(
            currentDebt: 0m,
            projectedDebt: 50m,
            averageDebt: 0m);

        Assert.Equal(LoanRiskType.ProjectedHighRisk, result);
    }

    [Theory]
    [InlineData(-1, 0, 0, "currentDebt")]
    [InlineData(10, 9, 0, "projectedDebt")]
    [InlineData(0, 0, -1, "averageDebt")]
    public void Invalid_debt_values_are_rejected(
        decimal currentDebt,
        decimal projectedDebt,
        decimal averageDebt,
        string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoanRiskPolicy.Evaluate(currentDebt, projectedDebt, averageDebt));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
