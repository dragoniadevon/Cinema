using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Pricecategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Sessionprice> Sessionprices { get; set; } = new List<Sessionprice>();
}
