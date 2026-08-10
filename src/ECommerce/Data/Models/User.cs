using Microsoft.AspNetCore.Identity;

namespace ECommerce.Data.Models;

public enum UserRole
{
    Admin,
    Customer
}

public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public DateTime CreatedAt { get; set; }

    public int? CartId { get; set; }
    public Cart? Cart { get; set; }

    public List<Review> Reviews { get; set; } = new List<Review>();

    public List<Order> Orders { get; set; } = new List<Order>();

    public List<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
}
