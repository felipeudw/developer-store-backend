using AutoMapper;
using Sales.Api.Features.Sales;
using Sales.Domain.Entities;

namespace Sales.Api.Features.Sales
{
    public sealed class SalesMappingProfile : Profile
    {
        public SalesMappingProfile()
        {
            CreateMap<SaleItem, SaleItemResponse>();

            CreateMap<Sale, SaleResponse>()
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
        }
    }
}