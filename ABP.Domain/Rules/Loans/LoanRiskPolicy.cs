using ABP.Domain.Enums;

namespace ABP.Domain.Rules.Loans;

public static class LoanRiskPolicy
{
    public static LoanRiskType Evaluate(
        decimal currentDebt,
        decimal projectedDebt,
        decimal averageDebt)
    {
        if (currentDebt < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentDebt),
                currentDebt,
                "La deuda actual no puede ser negativa.");
        }

        if (projectedDebt < currentDebt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectedDebt),
                projectedDebt,
                "La deuda proyectada no puede ser menor que la deuda actual.");
        }

        if (averageDebt < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(averageDebt),
                averageDebt,
                "La deuda promedio no puede ser negativa.");
        }

        if (currentDebt > averageDebt)
        {
            return LoanRiskType.CurrentHighRisk;
        }

        return projectedDebt > averageDebt
            ? LoanRiskType.ProjectedHighRisk
            : LoanRiskType.None;
    }
}
