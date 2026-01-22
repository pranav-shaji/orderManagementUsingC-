using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementSystem.Api.Entities

{
    public class Product
    {
        [Key] 
        public int Id { get; set; }

        [MaxLength(50)]
        public string Brand { get; set; } = "Unknown";
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName ="decimal(18,2)")]
        public decimal Price { get; set; }
        public int StockQuantity {  get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt {  get; set; }= DateTime.UtcNow;
    }
}
