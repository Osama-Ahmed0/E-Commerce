using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, string? search, string? sort, int? pageNumber, int? pageSize);
        Task<ServiceResult<ProductResponseDto>> GetProductByIdAsync(int id);
        Task<ServiceResult<ProductResponseDto>> CreateProductAsync(ProductDto dto);
        Task<ServiceResult<ProductResponseDto>> UpdateProductAsync(int id, ProductDto dto);
        Task<ServiceResult<bool>> DeleteProductAsync(int id);
    }
}