using ABP.Domain.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories;

public class LoanRepository(AppDbContext context) : GenericRepository<Loan, Guid>(context), ILoanRepository
{
    public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().FirstOrDefaultAsync(
            loan => loan.LoanNumber == loanNumber,
            cancellationToken);
    }

    public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(loan => loan.Client)
            .Include(loan => loan.Installments.OrderBy(installment => installment.Number))
            .SingleOrDefaultAsync(loan => loan.Id == id, cancellationToken);
    }

    public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().FirstOrDefaultAsync(
            loan => loan.ClientId == clientId && loan.Status == LoanStatus.Active,
            cancellationToken);
    }

    public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().AnyAsync(
            loan => loan.ClientId == clientId && loan.Status == LoanStatus.Active,
            cancellationToken);
    }

    public async Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var debt = await Entities
            .AsNoTracking()
            .Where(loan =>
                loan.ClientId == clientId
                && loan.Status == LoanStatus.Active)
            .Select(loan => (decimal?)loan.PendingAmount)
            .SumAsync(cancellationToken);

        return debt ?? 0m;
    }

    public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().AnyAsync(
            loan => loan.LoanNumber == loanNumber,
            cancellationToken);
    }

    public async Task<PagedResult<Loan>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatus? status = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPage = Math.Max(request.Page, 1);
        var normalizedPageSize = Math.Clamp(request.PageSize, 1, 20);
        var normalizedIdentification = clientIdentification?.Trim();
        var selectedStatus = status ?? LoanStatus.Active;

        var query = Entities
            .AsNoTracking()
            .Include(loan => loan.Client)
            .Where(loan =>
                loan.Client.Role == Roles.Client
                && loan.Status == selectedStatus);

        if (!string.IsNullOrWhiteSpace(normalizedIdentification))
        {
            query = query.Where(
                loan => loan.Client.Identification == normalizedIdentification);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(
            (long)(normalizedPage - 1) * normalizedPageSize,
            int.MaxValue);
        var data = await query
            .OrderByDescending(loan => loan.CreatedAtUtc)
            .Skip(skip)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Loan>(
            data,
            normalizedPage,
            normalizedPageSize,
            totalRecords);
    }

    public async Task<IReadOnlyCollection<LoanInstallment>> GetInstallmentsForDelinquencyUpdateAsync(DateOnly bankingDate, CancellationToken cancellationToken = default)
    {
        return await _context.LoanInstallments
            .Where(installment =>
                (installment.Loan.Status == LoanStatus.Active
                    && installment.DueDate < bankingDate
                    && installment.PendingAmount > 0m
                    && !installment.IsLate)
                || (installment.IsLate && installment.PendingAmount == 0m))
            .OrderBy(installment => installment.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default)
    {
        await _context.LoanInstallments.AddRangeAsync(installments, cancellationToken);
    }

    public async Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default)
    {
        await _context.LoanPayments.AddAsync(payment, cancellationToken);
    }
}
