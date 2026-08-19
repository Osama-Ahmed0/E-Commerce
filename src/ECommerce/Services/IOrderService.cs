using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<OrderDto>> CreateOrder(string userId, string shippingAddress);
        Task<ServiceResult<List<OrderDto>>> GetOrders(string userId);
        Task<ServiceResult<List<OrderDto>>> GetOrders();
        Task<ServiceResult<OrderDto>> GetOrderById(int orderId);
        Task<ServiceResult<OrderDto>> UpdateOrderStatus(int orderId, string status);
        Task<ServiceResult<bool>> CancelOrder(int orderId);
    }
}