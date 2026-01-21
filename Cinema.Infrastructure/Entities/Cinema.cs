using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Cinema
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? City { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
}
