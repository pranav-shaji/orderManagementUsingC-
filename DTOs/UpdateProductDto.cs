namespace OrderManagementSystem.Api.DTOs
{
    public class UpdateProductDto
    {
        public string Name { get; set; }=string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }
}
