using System.ComponentModel.DataAnnotations;

namespace ECommerce.Dtos
{
    public class CategoryDto
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int? ParentCategoryId { get; set; }
    }
}
