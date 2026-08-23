using System.ComponentModel.DataAnnotations;

namespace ECommerce.Dtos
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string OrderStatus { get; set; } = string.Empty;
    }
}
