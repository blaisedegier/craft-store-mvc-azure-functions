using System;
using System.Collections.Generic;

namespace ServerlessFunc.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? ProductId { get; set; }

    public string? UserId { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public virtual Product? Product { get; set; }

    public virtual AspNetUser? User { get; set; }
}
