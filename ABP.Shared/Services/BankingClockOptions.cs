namespace ABP.Shared.Services;

public sealed class BankingClockOptions
{
    public const string SectionName = "BankingTime";

    public string TimeZoneId { get; set; } = "America/La_Paz";
}
