namespace ECommerce.Data.Models;

public class CartItem
{
    public string CartId { get; set; } = string.Empty;
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}

