using GearShop.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GearShop.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Order)
                .WithMany(o => o.Comments)
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public virtual DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public virtual DbSet<Product> products { get; set; }
        public virtual DbSet<Brand> brands { get; set; }
        public virtual DbSet<Cart> carts { get; set; }
        public virtual DbSet<Order> orders { get; set; }
        public virtual DbSet<Comment> comments { get; set; }
        public virtual DbSet<ProductImage> productImages { get; set; }
        public virtual DbSet<ProductType> productTypes { get; set; }
        public virtual DbSet<Coupon> coupons { get; set; }
        public virtual DbSet<OrderStatus> orderStatuses { get; set; }
    }
}
