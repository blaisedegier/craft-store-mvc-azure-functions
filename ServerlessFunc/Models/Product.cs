using System;
using System.Collections.Generic;

namespace ServerlessFunc.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? Price { get; set; }

    public string? Availability { get; set; }

    public string? Image { get; set; }

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
