using ECommerce.Data.Models;

namespace ECommerce.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public required string ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<OrderItemDto> Items { get; set; } = [];
    }
}