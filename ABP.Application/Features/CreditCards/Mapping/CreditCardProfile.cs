using System.Globalization;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.CreditCards;
using AutoMapper;

namespace ABP.Application.Features.CreditCards.Mapping;

public sealed class CreditCardProfile : Profile
{
    public CreditCardProfile()
    {
        CreateMap<CreditCardSummaryReadModel, CreditCardSummaryDto>()
            .ForCtorParam(
                nameof(CreditCardSummaryDto.ExpirationDate),
                options => options.MapFrom(source => FormatExpiration(source.ExpirationDate)))
            .ForCtorParam(
                nameof(CreditCardSummaryDto.Status),
                options => options.MapFrom(source => MapCardStatus(source.Status)));

        CreateMap<CardConsumptionReadModel, CardConsumptionDto>()
            .ForCtorParam(
                nameof(CardConsumptionDto.Status),
                options => options.MapFrom(source => MapConsumptionStatus(source.Status)));

        CreateMap<CreditCardDetailReadModel, CreditCardDetailDto>()
            .ForCtorParam(
                nameof(CreditCardDetailDto.ExpirationDate),
                options => options.MapFrom(source => FormatExpiration(source.ExpirationDate)))
            .ForCtorParam(
                nameof(CreditCardDetailDto.Status),
                options => options.MapFrom(source => MapCardStatus(source.Status)))
            .ForCtorParam(
                nameof(CreditCardDetailDto.Consumptions),
                options => options.MapFrom(source => source.Consumptions));
    }

    private static string FormatExpiration(DateOnly expirationDate) =>
        expirationDate.ToString("MM/yy", CultureInfo.InvariantCulture);

    private static string MapCardStatus(CreditCardStatus status) => status switch
    {
        CreditCardStatus.Active => "Activa",
        CreditCardStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    private static string MapConsumptionStatus(ConsumptionStatus status) => status switch
    {
        ConsumptionStatus.Approved => "APROBADO",
        ConsumptionStatus.Rejected => "RECHAZADO",
        _ => status.ToString().ToUpperInvariant()
    };
}
