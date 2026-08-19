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

            CreateMap<Product, ProductResponseDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name));
            CreateMap<ProductDto, Product>();
            CreateMap<Product, ProductDto>();

            CreateMap<Category, CategoryResponseDto>()
                .ForMember(d => d.ParentCategoryName, o => o.MapFrom(s => s.ParentCategory.Name));
            CreateMap<CategoryDto, Category>();
            CreateMap<Category, CategoryDto>();

            CreateMap<CartItem, CartItemDto>().ReverseMap();

            CreateMap<Review, ReviewDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName));
            CreateMap<WriteReviewDto, Review>();
        }
    }
}
