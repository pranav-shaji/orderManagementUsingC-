using System.ComponentModel.DataAnnotations;
namespace OrderManagementSystem.Api.DTOs
{
    public class CreateProductDto
    {
        [MaxLength(50)]
        public string Brand { get; set; } = "Unknown";
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; }
    }
}
