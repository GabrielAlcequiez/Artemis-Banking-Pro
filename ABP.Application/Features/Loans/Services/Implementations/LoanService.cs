using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;
using FluentValidation;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanService(
    ILoanRepository repository,
    IMapper mapper,
    IValidator<LoanListRequest> listValidator) : ILoanService
{
    public async Task<PagedResult<LoanSummaryDto>> ListAsync(
        LoanListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await listValidator.ValidateAndThrowAsync(request, cancellationToken);

        var identification = NormalizeIdentification(request.Identification);
        var page = await repository.GetPagedAsync(
            new PagedRequest(request.Page, request.PageSize),
            identification,
            request.Status,
            cancellationToken);
        var data = mapper.Map<IReadOnlyCollection<LoanSummaryDto>>(page.Data);

        return new PagedResult<LoanSummaryDto>(
            data,
            page.Page,
            page.PageSize,
            page.TotalRecords);
    }

    public async Task<LoanDetailDto?> GetDetailAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan = await repository.GetDetailsAsync(
            loanId,
            cancellationToken);

        return loan is null
            ? null
            : mapper.Map<LoanDetailDto>(loan);
    }

    private static string? NormalizeIdentification(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
