using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using OrderManagementSystem.Api.Entities;
namespace OrderManagementSystem.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options)
            :base(options) 
        { 
           
        }
        public DbSet<Product> Products{ get; set; }
        // DbSets will be added here later
        // Example:
        // public DbSet<Product> Products { get; set; }
    }
}
