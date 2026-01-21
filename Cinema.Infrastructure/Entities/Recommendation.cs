using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Recommendation
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public int? Movieid { get; set; }

    public string? Reason { get; set; }

    public DateTime? Createdat { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual User? User { get; set; }
}
