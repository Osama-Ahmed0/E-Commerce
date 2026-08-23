using System.ComponentModel.DataAnnotations;

namespace ECommerce.Dtos
{
    public class WriteReviewDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
