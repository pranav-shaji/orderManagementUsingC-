using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Api.Data;
using OrderManagementSystem.Api.DTOs;
using OrderManagementSystem.Api.Entities;

namespace OrderManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPost("bulk-create")]
        public async Task<IActionResult> BulkCreateProducts([FromBody] List<CreateProductDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return BadRequest("The product list is empty.");
            }

            // Convert the List of DTOs into a List of Product Entities
            var productsToAdd = dtos.Select(dto => new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            // Use AddRangeAsync to add the entire list at once
            await _context.Products.AddRangeAsync(productsToAdd);

            // SaveChangesAsync only needs to be called ONCE at the end
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{productsToAdd.Count} products added successfully!" });
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts(int pageNumber = 1, int pageSize = 10)
        {
            // 1. Ensure pageNumber and pageSize are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // 2. Calculate how many items to skip
            // Page 1: (1-1) * 10 = Skip 0
            // Page 2: (2-1) * 10 = Skip 10
            int skip = (pageNumber - 1) * pageSize;

            // 3. Get the total count (Useful for the frontend to show "Total Pages")
            var totalRecords = await _context.Products.CountAsync(p => p.IsActive);

            // 4. Query the database using Skip and Take
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Id) // Pagination REQUIRES sorting to be consistent
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // 5. Return the data along with "Metadata"
            return Ok(new
            {
                TotalCount = totalRecords,
                Page = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Data = products
            });
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null || !product.IsActive)
                return NotFound();

            return Ok(product);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockAlert([FromQuery]int threshold = 5)
        {
            var lowStockProducts = await _context.Products.Where(p => p.IsActive && p.StockQuantity <= threshold).Select(p => new
            {
                p.Id,
                p.Name,
                p.StockQuantity,
                Status = p.StockQuantity == 0 ? "out of stock" : "low stock",
                ReorderUrgency = p.StockQuantity <= (threshold / 2) ? "high" : "medium"
            }).ToListAsync();
            var responce = new
            {
                AlertCount = lowStockProducts.Count,
                thresholdUsed = threshold,
                Products = lowStockProducts,
                TimeStamp = DateTime.UtcNow
            };
            return Ok(responce);
        }
        //[HttpPost("increase-price-by-the-brand")]
        //public async Task<IActionResult> IncreasePriceByBrand([FromQuery] string brand, [FromQuery] decimal = 5)
        //{
        //    if (string.IsNullOrWhiteSpace(brand))
        //        return BadRequest("brand name is required");

        //    decimal multiplier = 1 + (percentage / 100);
        //    //executeUpdateAsync
        //    int affectedRows = await _context.Products.Where(p => p.Name.Contains(brand) && p.IsActive)
        //        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Price, p => p.Price * multiplier));

        //    if (affectedRows = 0)
        //        return NotFound($"No active products found containing the brand name '{brand}'.");

        //    return Ok(new
        //    {
        //        Message = $"Sucessfully updated{affectedRows}products.",
        //        BrandTargeted = brand,
        //        IncreasePercentage = $"{percentage}"
        //    });
        //    
        // POST: api/products/increase-price-by-brand?brand=Samsung
        [HttpPost("increase-price-by-brand")]
        public async Task<IActionResult> IncreasePriceByBrand([FromQuery] string brand, [FromQuery] decimal percentage = 5)
        {
            if (string.IsNullOrWhiteSpace(brand))
                return BadRequest("Brand name is required.");

            // Convert percentage to a multiplier (e.g., 5% becomes 1.05)
            decimal multiplier = 1 + (percentage / 100);

            // .ExecuteUpdateAsync is a .NET 7/8 feature
            int affectedRows = await _context.Products
                .Where(p => p.Name.Contains(brand) && p.IsActive)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Price, p => p.Price * multiplier));

            if (affectedRows == 0)
                return NotFound($"No active products found containing the brand name '{brand}'.");

            return Ok(new
            {
                Message = $"Successfully updated {affectedRows} products.",
                BrandTargeted = brand,
                IncreasePercentage = $"{percentage}%"
            });
        }

        [HttpPost("auto-fix-brands")]
        public async Task<IActionResult> AutoFixBrands()
        {
            // 1. Look for Brand that is null, empty, or "Unknown"
            var products = await _context.Products
                .Where(p => string.IsNullOrEmpty(p.Brand) || p.Brand == "Unknown")
                .ToListAsync();

            if (products.Count == 0)
                return Ok(new { message = "No products found that need a brand update." });

            foreach (var product in products)
            {
                // 2. Logic: Take the first word of the Name as the Brand
                // Example: "Samsung Galaxy A54" -> "Samsung"
                var nameParts = product.Name.Trim().Split(' ');
                if (nameParts.Length > 0)
                {
                    product.Brand = nameParts[0];
                }
            }

            // 3. Save the changes
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully updated brands for {products.Count} products." });
        }

        // DELETE: api/products/{id}(Soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

