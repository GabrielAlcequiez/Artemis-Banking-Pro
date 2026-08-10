using ABP.Domain.Enums;

namespace ABP.Domain.Rules.Cards;

public static class CreditCardRules
{
    public static bool IsCreditLimitValid(decimal creditLimit)
    {
        return creditLimit > 0m;
    }

    public static DateOnly CalculateExpirationDate(DateOnly bankingDate)
    {
        var expirationYear = bankingDate.Year + 3;
        var lastDay = DateTime.DaysInMonth(
            expirationYear,
            bankingDate.Month);

        return new DateOnly(
            expirationYear,
            bankingDate.Month,
            lastDay);
    }

    public static bool IsExpired(
        DateOnly expirationDate,
        DateOnly bankingDate)
    {
        return bankingDate > expirationDate;
    }

    public static bool CanChangeLimit(
        CreditCardStatus status,
        decimal currentDebt,
        decimal newLimit)
    {
        return status == CreditCardStatus.Active
            && IsCreditLimitValid(newLimit)
            && newLimit >= currentDebt;
    }

    public static bool CanCancel(
        CreditCardStatus status,
        decimal currentDebt)
    {
        return status == CreditCardStatus.Active
            && currentDebt == 0m;
    }
}
