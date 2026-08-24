using System.ComponentModel.DataAnnotations;

namespace ECommerce.Dtos
{
    public class UserLoginDto
    {
        [Required, MaxLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required, MaxLength(100), MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;
    }
}
