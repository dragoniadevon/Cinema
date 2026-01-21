using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Seat
{
    public int Id { get; set; }

    public int? Hallid { get; set; }

    public short? Rownumber { get; set; }

    public short? Seatnumber { get; set; }

    public virtual Hall? Hall { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
