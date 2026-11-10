using Microsoft.AspNetCore.Identity;

namespace CraftMarket.Models;

public class Order
{
    public int OrderId { get; set; }

    public int? ProductId { get; set; }

    public string? UserId { get; set; }

    public string OrderStatus { get; set; } = OrderStatuses.Pending;

    public string PaymentStatus { get; set; } = OrderStatuses.Pending;

    public virtual Product? Product { get; set; }

    public virtual IdentityUser? User { get; set; }
}

// The values written to OrderStatus and PaymentStatus. The Functions app writes the same strings.
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Expired = "Expired";
}
