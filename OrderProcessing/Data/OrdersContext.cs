using Microsoft.EntityFrameworkCore;

namespace OrderProcessing.Data;

// The Functions app only ever changes an order's two status columns, so it maps just those columns
// of the web app's Orders table instead of referencing the web project and its whole model.
public class OrdersContext : DbContext
{
    public OrdersContext(DbContextOptions<OrdersContext> options)
        : base(options)
    {
    }

    public DbSet<OrderRecord> Orders => Set<OrderRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderRecord>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.OrderStatus).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
        });
    }
}

public class OrderRecord
{
    public int OrderId { get; set; }

    public string OrderStatus { get; set; } = OrderStatuses.Pending;

    public string PaymentStatus { get; set; } = OrderStatuses.Pending;
}

// The same status strings the web app uses (CraftMarket.Models.OrderStatuses).
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Expired = "Expired";
}
