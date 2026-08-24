using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<OrderDto>> CreateOrderAsync(string userId, CheckoutRequestDto dto);
        Task<ServiceResult<List<OrderDto>>> GetOrdersAsync(string userId);
        Task<ServiceResult<List<OrderDto>>> GetOrdersAsync();
        Task<ServiceResult<OrderDto>> GetOrderByIdAsync(int orderId);
        Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto);
        Task<ServiceResult<bool>> CancelOrderAsync(int orderId);
    }
}