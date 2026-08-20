using ABP.Application.Common;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using MediatR;

namespace ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;

public sealed class GetHermesTransactionsQueryHandler(
    ICommerceAuthorizationResolverService authorizationResolver,
    ICommerceRepository commerceRepository,
    IHermesTransactionRepository transactionRepository)
    : IRequestHandler<GetHermesTransactionsQuery, OperationResult<HermesTransactionsPageDto>>
{
    public async Task<OperationResult<HermesTransactionsPageDto>> Handle(
        GetHermesTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizationResolver.ResolveAuthorizedCommerceIdAsync(
            query.RequestedCommerceId,
            cancellationToken);

        if (authorization.IsFailure)
        {
            return OperationResult<HermesTransactionsPageDto>.Failure(
                authorization.Error);
        }

        var commerceId = authorization.Value;
        var commerce = await commerceRepository.GetDetailsAsync(
            commerceId,
            cancellationToken);

        if (commerce is null)
        {
            return OperationResult<HermesTransactionsPageDto>.Failure(
                HermesPayErrors.CommerceNotFound);
        }

        var page = await transactionRepository.GetByCommerceAsync(
            commerceId,
            query.Page,
            query.PageSize,
            cancellationToken);
        var data = page.Data
            .Select(transaction => new HermesTransactionDto(
                transaction.Id,
                transaction.TransactionDate,
                transaction.Amount,
                transaction.CardLastFourDigits,
                ToApiStatus(transaction.Status)))
            .ToArray();

        return OperationResult<HermesTransactionsPageDto>.Success(
            new HermesTransactionsPageDto(
                page.Page,
                page.PageSize,
                page.TotalRecords,
                page.TotalPages,
                commerceId,
                commerce.Name,
                data));
    }

    private static string ToApiStatus(ConsumptionStatus status) => status switch
    {
        ConsumptionStatus.Approved => "APROBADO",
        ConsumptionStatus.Rejected => "RECHAZADO",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
