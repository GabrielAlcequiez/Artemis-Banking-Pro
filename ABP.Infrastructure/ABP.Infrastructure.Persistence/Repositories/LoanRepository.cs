using ABP.Domain.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
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

    public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(loan => loan.Client)
            .Include(loan => loan.Installments.OrderBy(installment => installment.Number))
            .SingleOrDefaultAsync(loan => loan.Id == id, cancellationToken);
    }

    public Task<Loan?> GetDetailsForClientAsync(
        Guid id,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(loan => loan.Client)
            .Include(loan => loan.Installments.OrderBy(installment => installment.Number))
            .SingleOrDefaultAsync(
                loan => loan.Id == id && loan.ClientId == clientId,
                cancellationToken);
    }

    public Task<LoanPayment?> GetPaymentByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        return _context.LoanPayments
            .AsNoTracking()
            .Include(payment => payment.Loan)
            .SingleOrDefaultAsync(
                payment => payment.OperationId == operationId,
                cancellationToken);
    }

    public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().FirstOrDefaultAsync(
            loan => loan.ClientId == clientId && loan.Status == LoanStatus.Active,
            cancellationToken);
    }

    public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(loan =>
                loan.ClientId == clientId
                && loan.Status == LoanStatus.Active)
            .Select(loan => new ClientLoanPortfolioReadModel(
                loan.Id,
                loan.LoanNumber,
                loan.Capital,
                loan.PendingAmount,
                loan.Installments
                    .OrderBy(installment => installment.Number)
                    .Select(installment => installment.InstallmentAmount)
                    .FirstOrDefault(),
                loan.AnnualInterestRate,
                loan.TermInMonths))
            .SingleOrDefaultAsync(cancellationToken);
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

    public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        return await Entities
            .AsNoTracking()
            .Where(loan =>
                clientIds.Contains(loan.ClientId)
                && loan.Status == LoanStatus.Active)
            .GroupBy(loan => loan.ClientId)
            .Select(group => new
            {
                ClientId = group.Key,
                Debt = group.Sum(loan => loan.PendingAmount)
            })
            .ToDictionaryAsync(
                item => item.ClientId,
                item => item.Debt,
                cancellationToken);
    }

    public async Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var debt = await Entities
            .AsNoTracking()
            .Where(loan =>
                loan.Status == LoanStatus.Active
                && loan.Client.Role == Roles.Client
                && loan.Client.IsActive)
            .Select(loan => (decimal?)loan.PendingAmount)
            .SumAsync(cancellationToken);

        return debt ?? 0m;
    }

    public async Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(
        PagedRequest request,
        string? clientIdentification = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPage = Math.Max(request.Page, 1);
        var normalizedPageSize = Math.Clamp(request.PageSize, 1, 20);
        var normalizedIdentification = clientIdentification?.Trim();

        var query = _context.Users
            .AsNoTracking()
            .Where(client =>
                client.Role == Roles.Client
                && client.IsActive
                && !_context.Loans.Any(loan =>
                    loan.ClientId == client.Id
                    && loan.Status == LoanStatus.Active));

        if (!string.IsNullOrWhiteSpace(normalizedIdentification))
        {
            query = query.Where(
                client => client.Identification == normalizedIdentification);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(
            (long)(normalizedPage - 1) * normalizedPageSize,
            int.MaxValue);
        var data = await query
            .OrderBy(client => client.Identification)
            .ThenBy(client => client.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(client => new LoanClientCandidateReadModel(
                client.Id,
                client.Identification,
                client.Name + " " + client.LastName,
                client.Email))
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanClientCandidateReadModel>(
            data,
            normalizedPage,
            normalizedPageSize,
            totalRecords);
    }

    public async Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .Where(client =>
                client.Id == clientId
                && client.Role == Roles.Client
                && client.IsActive
                && !_context.Loans.Any(loan =>
                    loan.ClientId == client.Id
                    && loan.Status == LoanStatus.Active))
            .Select(client => new LoanClientCandidateReadModel(
                client.Id,
                client.Identification,
                client.Name + " " + client.LastName,
                client.Email))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountActiveLoansAsync(
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .CountAsync(
                loan => loan.Status == LoanStatus.Active,
                cancellationToken);
    }

    public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default)
    {
        return Entities.AsNoTracking().AnyAsync(
            loan => loan.LoanNumber == loanNumber,
            cancellationToken);
    }

    public async Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(
        PagedRequest request,
        string? clientIdentification = null,
        LoanStatusFilter? status = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPage = Math.Max(request.Page, 1);
        var normalizedPageSize = Math.Clamp(request.PageSize, 1, 20);
        var normalizedIdentification = clientIdentification?.Trim();
        var hasIdentification = !string.IsNullOrWhiteSpace(normalizedIdentification);

        var query = Entities
            .AsNoTracking()
            .Where(loan => loan.Client.Role == Roles.Client);

        if (hasIdentification)
        {
            query = query.Where(
                loan => loan.Client.Identification == normalizedIdentification!);
        }

        switch (status)
        {
            case null:
                if (!hasIdentification)
                {
                    query = query.Where(loan => loan.Status == LoanStatus.Active);
                }
                break;
            case LoanStatusFilter.Active:
                query = query.Where(loan => loan.Status == LoanStatus.Active);
                break;
            case LoanStatusFilter.Completed:
                query = query.Where(loan => loan.Status == LoanStatus.Completed);
                break;
            case LoanStatusFilter.All:
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "El filtro de estado del préstamo no es válido.");
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var includeAllStatuses = status == LoanStatusFilter.All
            || (status is null && hasIdentification);
        var orderedQuery = includeAllStatuses
            ? query
                .OrderBy(loan => loan.Status == LoanStatus.Active ? 0 : 1)
                .ThenByDescending(loan => loan.CreatedAtUtc)
            : query.OrderByDescending(loan => loan.CreatedAtUtc);
        var skip = (int)Math.Min(
            (long)(normalizedPage - 1) * normalizedPageSize,
            int.MaxValue);
        var data = await orderedQuery
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(loan => new LoanSummaryReadModel(
                loan.Id,
                loan.LoanNumber,
                loan.ClientId,
                loan.Client.Name + " " + loan.Client.LastName,
                loan.Capital,
                loan.Installments.Count,
                loan.Installments.Count(installment =>
                    installment.PaymentStatus == InstallmentPaymentStatus.Paid),
                loan.PendingAmount,
                loan.AnnualInterestRate,
                loan.TermInMonths,
                loan.Status,
                loan.Installments.Any(installment =>
                    installment.IsLate && installment.PendingAmount > 0m),
                loan.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanSummaryReadModel>(
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
