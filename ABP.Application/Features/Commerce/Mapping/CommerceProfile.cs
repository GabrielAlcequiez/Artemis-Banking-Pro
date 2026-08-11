using ABP.Application.Features.Commerce.DTOs;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Commerce;
using AutoMapper;

namespace ABP.Application.Features.Commerce.Mapping;

public sealed class CommerceProfile : Profile
{
    public CommerceProfile()
    {
        CreateMap<AssociatedCommerceUserReadModel, AssociatedCommerceUserDto>();

        CreateMap<CommerceSummaryReadModel, CommerceSummaryDto>()
            .ForCtorParam(
                nameof(CommerceSummaryDto.IsActive),
                options => options.MapFrom(source => source.Status == CommerceStatus.Active));

        CreateMap<CommerceDetailReadModel, CommerceDetailDto>()
            .ForCtorParam(
                nameof(CommerceDetailDto.IsActive),
                options => options.MapFrom(source => source.Status == CommerceStatus.Active))
            .ForCtorParam(
                nameof(CommerceDetailDto.AssociatedUser),
                options => options.MapFrom(source => source.AssociatedUser));
    }
}
