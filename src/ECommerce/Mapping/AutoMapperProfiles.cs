using AutoMapper;
using ECommerce.Data.Models;
using ECommerce.Dtos;

namespace ECommerce.Mapping
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.UnitPriceAtPurchase, o => o.MapFrom(s => s.UnitPrice));

            CreateMap<Order, OrderDto>()
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.OrderDate))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.OrderItems));
        }
    }
}
