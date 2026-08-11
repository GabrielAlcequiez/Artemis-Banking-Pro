using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Mapping;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Mapping;

public sealed class LoanProfileTests
{
    private readonly IMapper mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<LoanProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Configuration_is_valid()
    {
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Maps_loan_summary_with_counts_and_delinquency_status()
    {
        var loan = CreateLoan();
        loan.Installments =
        [
            CreateInstallment(1, InstallmentPaymentStatus.Paid, 0m),
            CreateInstallment(2, InstallmentPaymentStatus.Pending, 125m, isLate: true),
            CreateInstallment(3, InstallmentPaymentStatus.Pending, 125m)
        ];

        var result = mapper.Map<LoanSummaryDto>(loan);

        Assert.Equal("Ana P?rez", result.ClientFullName);
        Assert.Equal(1_000m, result.CapitalAmount);
        Assert.Equal(3, result.TotalInstallments);
        Assert.Equal(1, result.PaidInstallments);
        Assert.Equal("Activo", result.Status);
        Assert.Equal("En mora", result.ClientPaymentStatus);
    }

    [Fact]
    public void Maps_loan_detail_with_ordered_amortization_and_spanish_statuses()
    {
        var loan = CreateLoan();
        loan.Installments =
        [
            CreateInstallment(2, InstallmentPaymentStatus.PartiallyPaid, 25m),
            CreateInstallment(1, InstallmentPaymentStatus.Pending, 100m)
        ];

        var result = mapper.Map<LoanDetailDto>(loan);

        Assert.Equal(100m, result.MonthlyInstallment);
        Assert.Equal("Al d?a", result.ClientPaymentStatus);
        Assert.Equal([1, 2], result.Amortization.Select(item => item.InstallmentNumber));
        Assert.Equal("Pendiente", result.Amortization.First().PaymentStatus);
        Assert.Equal("Parcialmente pagada", result.Amortization.Last().PaymentStatus);
        Assert.Equal(25m, result.Amortization.Last().PendingInstallmentAmount);
    }

    [Fact]
    public void Maps_completed_loan_as_paid_off()
    {
        var loan = CreateLoan();
        loan.Status = LoanStatus.Completed;
        loan.PendingAmount = 0m;
        loan.Installments =
        [
            CreateInstallment(1, InstallmentPaymentStatus.Paid, 0m)
        ];

        var result = mapper.Map<LoanSummaryDto>(loan);

        Assert.Equal("Completado", result.Status);
        Assert.Equal("Saldado", result.ClientPaymentStatus);
    }

    private static Loan CreateLoan() => new()
    {
        ClientId = "client-1",
        Client = new User("client-1")
        {
            Name = "Ana",
            LastName = "P?rez"
        },
        LoanNumber = "123456789",
        Capital = 1_000m,
        PendingAmount = 250m,
        AnnualInterestRate = 12m,
        TermInMonths = 12,
        Status = LoanStatus.Active,
        AssignedByUserId = "admin-1"
    };

    private static LoanInstallment CreateInstallment(
        int number,
        InstallmentPaymentStatus paymentStatus,
        decimal pendingAmount,
        bool isLate = false) => new()
    {
        Number = number,
        DueDate = new DateOnly(2026, 9, 10).AddMonths(number - 1),
        InstallmentAmount = 100m,
        InterestAmount = 10m,
        CapitalAmount = 90m,
        PendingAmount = pendingAmount,
        PaymentStatus = paymentStatus,
        IsLate = isLate
    };
}
