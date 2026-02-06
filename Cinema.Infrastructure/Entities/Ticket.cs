using System;
using Cinema.Infrastructure.Entities;

namespace Cinema.Infrastructure.Entities;

public partial class Ticket
{
    public int Id { get; set; }

    // 🔐 Identity user
    public int Userid { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public int Sessionid { get; set; }
    public virtual Session Session { get; set; } = null!;

    public int Seatid { get; set; }
    public virtual Seat Seat { get; set; } = null!;

    public decimal Price { get; set; }

    public short? Status { get; set; }

    public DateTime Bookingtime { get; set; }

    public virtual Payment? Payment { get; set; }

    public bool IsReturned { get; set; } = false;

}
