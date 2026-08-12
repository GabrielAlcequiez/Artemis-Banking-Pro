using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Common.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Rules.Loans;
using FluentValidation;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanRiskService(
    ICustomerDebtService customerDebtService,
    IAmortizationCalculator amortizationCalculator,
    IClock clock,
    IValidator<CreateLoanRequest> requestValidator)
    : ILoanRiskService
{
    public async Task<HighRiskAssessmentDto> AssessAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var amortization = amortizationCalculator.Calculate(
            request.CapitalAmount,
            request.AnnualInterestRate,
            request.TermInMonths,
            clock.Today);
        var currentDebt = await customerDebtService.GetTotalDebtAsync(
            request.ClientId,
            cancellationToken);
        var averageDebt = await customerDebtService.GetAverageActiveClientDebtAsync(
            cancellationToken);
        var projectedDebt = currentDebt + amortization.TotalAmountToPay;
        var riskType = LoanRiskPolicy.Evaluate(
            currentDebt,
            projectedDebt,
            averageDebt);
        var requiresConfirmation = riskType != LoanRiskType.None
            && !request.ConfirmHighRisk;

        return new HighRiskAssessmentDto(
            riskType.ToString(),
            currentDebt,
            projectedDebt,
            averageDebt,
            requiresConfirmation);
    }
}
