using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Ticket
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public int? Sessionid { get; set; }

    public int? Seatid { get; set; }

    public decimal Price { get; set; }

    public short? Status { get; set; }

    public DateTime? Bookingtime { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual Seat? Seat { get; set; }

    public virtual Session? Session { get; set; }

    public virtual User? User { get; set; }
}
