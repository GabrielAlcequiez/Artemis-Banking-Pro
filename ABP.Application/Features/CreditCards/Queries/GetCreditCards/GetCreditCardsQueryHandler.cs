using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.CreditCards.Queries.GetCreditCards;

public sealed class GetCreditCardsQueryHandler(
    ICreditCardRepository repository,
    IMapper mapper) : IRequestHandler<GetCreditCardsQuery, CreditCardListResult>
{
    public async Task<CreditCardListResult> Handle(
        GetCreditCardsQuery query,
        CancellationToken cancellationToken)
    {
        var request = query.Request;
        var identification = NormalizeIdentification(request.Identification);
        var normalizedRequest = request with { Identification = identification };

        if (identification is not null)
        {
            var clientId = await repository.FindClientIdByIdentificationAsync(
                identification,
                cancellationToken);

            if (clientId is null)
            {
                return new(
                    CreateEmptyPage(request),
                    CreditCardSearchStatus.ClientNotFound);
            }

            if (!await repository.HasAnyCardsAsync(clientId, cancellationToken))
            {
                return new(
                    CreateEmptyPage(request),
                    CreditCardSearchStatus.ClientWithoutCards);
            }
        }

        var readPage = await repository.SearchAsync(
            normalizedRequest.Page,
            normalizedRequest.PageSize,
            normalizedRequest.Identification,
            normalizedRequest.Status,
            cancellationToken);

        var searchStatus = identification is null && !normalizedRequest.Status.HasValue
            ? CreditCardSearchStatus.NoSearch
            : readPage.TotalRecords == 0
                ? CreditCardSearchStatus.NoMatchingCards
                : CreditCardSearchStatus.ResultsFound;

        return new(MapPage(readPage), searchStatus);
    }

    private PagedResult<CreditCardSummaryDto> MapPage(
        PagedResult<CreditCardSummaryReadModel> page)
    {
        var data = mapper.Map<IReadOnlyCollection<CreditCardSummaryDto>>(page.Data);

        return new(data, page.Page, page.PageSize, page.TotalRecords);
    }

    private static PagedResult<CreditCardSummaryDto> CreateEmptyPage(
        CreditCardListRequest request) =>
        new(Array.Empty<CreditCardSummaryDto>(), request.Page, request.PageSize, 0);

    private static string? NormalizeIdentification(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
