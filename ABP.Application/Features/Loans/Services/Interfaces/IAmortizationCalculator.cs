using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface IAmortizationCalculator
{
    AmortizationResult Calculate(decimal capital, decimal annualInterestRate, int termInMonths, DateOnly creationDate);
}
