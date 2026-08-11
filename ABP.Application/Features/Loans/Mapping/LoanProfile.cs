using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using AutoMapper;

namespace ABP.Application.Features.Loans.Mapping;

public sealed class LoanProfile : Profile
{
    public LoanProfile()
    {
        CreateMap<LoanInstallment, LoanInstallmentDto>()
            .ForCtorParam(
                nameof(LoanInstallmentDto.InstallmentNumber),
                options => options.MapFrom(source => source.Number))
            .ForCtorParam(
                nameof(LoanInstallmentDto.PendingInstallmentAmount),
                options => options.MapFrom(source => source.PendingAmount))
            .ForCtorParam(
                nameof(LoanInstallmentDto.PaymentStatus),
                options => options.MapFrom(source => MapInstallmentStatus(source.PaymentStatus)));

        CreateMap<Loan, LoanSummaryDto>()
            .ForCtorParam(
                nameof(LoanSummaryDto.ClientFullName),
                options => options.MapFrom(source => GetClientFullName(source)))
            .ForCtorParam(
                nameof(LoanSummaryDto.CapitalAmount),
                options => options.MapFrom(source => source.Capital))
            .ForCtorParam(
                nameof(LoanSummaryDto.TotalInstallments),
                options => options.MapFrom(source => source.Installments.Count))
            .ForCtorParam(
                nameof(LoanSummaryDto.PaidInstallments),
                options => options.MapFrom(source => source.Installments.Count(
                    installment => installment.PaymentStatus == InstallmentPaymentStatus.Paid)))
            .ForCtorParam(
                nameof(LoanSummaryDto.Status),
                options => options.MapFrom(source => MapLoanStatus(source.Status)))
            .ForCtorParam(
                nameof(LoanSummaryDto.ClientPaymentStatus),
                options => options.MapFrom(source => MapClientPaymentStatus(source)))
            .ForCtorParam(
                nameof(LoanSummaryDto.CreatedAt),
                options => options.MapFrom(source => source.CreatedAtUtc));

        CreateMap<Loan, LoanDetailDto>()
            .ForCtorParam(
                nameof(LoanDetailDto.ClientFullName),
                options => options.MapFrom(source => GetClientFullName(source)))
            .ForCtorParam(
                nameof(LoanDetailDto.CapitalAmount),
                options => options.MapFrom(source => source.Capital))
            .ForCtorParam(
                nameof(LoanDetailDto.MonthlyInstallment),
                options => options.MapFrom(source => GetMonthlyInstallment(source)))
            .ForCtorParam(
                nameof(LoanDetailDto.Status),
                options => options.MapFrom(source => MapLoanStatus(source.Status)))
            .ForCtorParam(
                nameof(LoanDetailDto.ClientPaymentStatus),
                options => options.MapFrom(source => MapClientPaymentStatus(source)))
            .ForCtorParam(
                nameof(LoanDetailDto.CreatedAt),
                options => options.MapFrom(source => source.CreatedAtUtc))
            .ForCtorParam(
                nameof(LoanDetailDto.Amortization),
                options => options.MapFrom(source => source.Installments.OrderBy(
                    installment => installment.Number)));
    }

    private static string GetClientFullName(Loan loan) =>
        loan.Client is null
            ? string.Empty
            : $"{loan.Client.Name} {loan.Client.LastName}".Trim();

    private static decimal GetMonthlyInstallment(Loan loan) =>
        loan.Installments
            .OrderBy(installment => installment.Number)
            .Select(installment => installment.InstallmentAmount)
            .FirstOrDefault();

    private static string MapLoanStatus(LoanStatus status) => status switch
    {
        LoanStatus.Active => "Activo",
        LoanStatus.Completed => "Completado",
        _ => status.ToString()
    };

    private static string MapClientPaymentStatus(Loan loan)
    {
        if (loan.Status == LoanStatus.Completed || loan.PendingAmount == 0m)
        {
            return "Saldado";
        }

        return loan.Installments.Any(
            installment => installment.IsLate && installment.PendingAmount > 0m)
            ? "En mora"
            : "Al d?a";
    }

    private static string MapInstallmentStatus(InstallmentPaymentStatus status) => status switch
    {
        InstallmentPaymentStatus.Pending => "Pendiente",
        InstallmentPaymentStatus.PartiallyPaid => "Parcialmente pagada",
        InstallmentPaymentStatus.Paid => "Pagada",
        _ => status.ToString()
    };
}
