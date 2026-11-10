using CraftMarket.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CraftMarket.Data;

// One context holds both the Identity tables and the store's own tables, so an order can reference
// the user who placed it and one migration history creates the whole database.
public class CraftMarketContext : IdentityDbContext<IdentityUser>
{
    public CraftMarketContext(DbContextOptions<CraftMarketContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(256);
            entity.Property(e => e.Availability).HasMaxLength(256);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId);
        });

        // The Functions app maps this same table (OrderID, OrderStatus, PaymentStatus) in its own
        // small context, so keep those column names stable.
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.OrderStatus).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);

            entity.HasOne(d => d.Product).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId);
        });
    }
}
