namespace ECommerce.Dtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceAtPurchase { get; set; }
        public decimal Total => Quantity * UnitPriceAtPurchase;
    }
}