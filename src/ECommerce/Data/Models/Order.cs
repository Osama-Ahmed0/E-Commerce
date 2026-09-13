namespace ECommerce.Data.Models;

public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}