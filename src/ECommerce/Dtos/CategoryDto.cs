namespace ECommerce.Dtos
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } = null!;
        public int? ParentCategoryId { get; set; }
    }
}
