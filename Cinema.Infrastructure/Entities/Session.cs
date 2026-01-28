using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Session
{
    public int Id { get; set; }

    public int? Movieid { get; set; }

    public int? Hallid { get; set; }

    public DateTime Starttime { get; set; }

    public DateTime Endtime { get; set; }

    public short? Format { get; set; }

    public bool? Isactive { get; set; }

    public virtual Hall? Hall { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual ICollection<Sessionprice> Sessionprices { get; set; } = new List<Sessionprice>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
