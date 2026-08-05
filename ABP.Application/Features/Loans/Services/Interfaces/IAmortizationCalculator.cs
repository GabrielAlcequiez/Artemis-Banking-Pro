using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Interfaces.Services;

public interface IAmortizationCalculator
{
    AmortizationResult Calculate(decimal capital, decimal annualInterestRate, int termInMonths, DateOnly creationDate);
}
