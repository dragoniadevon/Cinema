using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Sessionprice
{
    public int Id { get; set; }

    public int? Sessionid { get; set; }

    public int? Categoryid { get; set; }

    public decimal Price { get; set; }

    public virtual Pricecategory? Category { get; set; }

    public virtual Session? Session { get; set; }
}
