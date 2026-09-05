namespace ECommerce.Dtos
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
