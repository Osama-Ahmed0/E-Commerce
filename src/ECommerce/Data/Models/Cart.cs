namespace ECommerce.Data.Models;

public class Cart
{
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public List<CartItem> CartItems { get; set; } = new List<CartItem>();
}

