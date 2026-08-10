using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class AmortizationCalculator : IAmortizationCalculator
{
    private const int MoneyDecimals = 2;
    private const string PendingPaymentStatus = "Pendiente";

    public AmortizationResult Calculate(decimal capital, decimal annualInterestRate, int termInMonths, DateOnly creationDate)
    {
        if (capital <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capital),
                capital,
                "El capital debe ser mayor que cero.");
        }

        if (annualInterestRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualInterestRate),
                annualInterestRate,
                "La tasa de interés anual no puede ser negativa.");
        }

        if (termInMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(termInMonths),
                termInMonths,
                "El plazo debe ser mayor que cero.");
        }

        var monthlyInterestRate = annualInterestRate / 100m / 12m;
        var exactMonthlyInstallment = CalculateMonthlyInstallment(capital, monthlyInterestRate, termInMonths);
        var monthlyInstallment = RoundMoney(exactMonthlyInstallment);

        var installments = new List<LoanInstallmentDto>(termInMonths);
        var pendingCapital = capital;

        for (var installmentNumber = 1; installmentNumber <= termInMonths; installmentNumber++)
        {
            var interestAmount = RoundMoney(pendingCapital * monthlyInterestRate);

            var capitalAmount = installmentNumber == termInMonths ? pendingCapital : RoundMoney(monthlyInstallment - interestAmount);

            var installmentAmount = installmentNumber == termInMonths ? RoundMoney(capitalAmount + interestAmount) : monthlyInstallment;

            installments.Add(new LoanInstallmentDto(installmentNumber, creationDate.AddMonths(installmentNumber), 
                installmentAmount, interestAmount, capitalAmount, installmentAmount, PendingPaymentStatus, false));

            pendingCapital = RoundMoney(pendingCapital - capitalAmount);
        }

        return new AmortizationResult(monthlyInstallment, RoundMoney(installments.Sum(x => x.InstallmentAmount)), installments);
    }

    private static decimal CalculateMonthlyInstallment(decimal capital, decimal monthlyInterestRate, int termInMonths)
    {
        if (monthlyInterestRate == 0)
        {
            return capital / termInMonths;
        }

        var compoundFactor = Pow(1m + monthlyInterestRate, termInMonths);

        return capital
            * (monthlyInterestRate * compoundFactor)
            / (compoundFactor - 1m);
    }

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;

        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
}
