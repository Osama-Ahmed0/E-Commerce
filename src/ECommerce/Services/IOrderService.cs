using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<OrderDto>> CreateOrderAsync(string userId, string shippingAddress);
        Task<ServiceResult<List<OrderDto>>> GetOrdersAsync(string userId);
        Task<ServiceResult<List<OrderDto>>> GetOrdersAsync();
        Task<ServiceResult<OrderDto>> GetOrderByIdAsync(int orderId);
        Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status);
        Task<ServiceResult<bool>> CancelOrderAsync(int orderId);
    }
}