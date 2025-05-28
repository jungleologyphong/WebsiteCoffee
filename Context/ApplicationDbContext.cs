using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<RewardPoints> RewardPoints { get; set; }

        // 👉 Thêm dòng này:
        public DbSet<DiscountCode> DiscountCodes { get; set; }

        public DbSet<CartItem> CartItems { get; set; }
    }
}