namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record HighRiskAssessmentDto(
        string RiskType,
        decimal CurrentDebt,
        decimal ProjectedDebt,
        decimal AverageDebt,
        bool RequiresConfirmation);
}
