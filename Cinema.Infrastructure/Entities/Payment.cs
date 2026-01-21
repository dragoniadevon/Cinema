using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Payment
{
    public int Id { get; set; }

    public int? Ticketid { get; set; }

    public decimal Amount { get; set; }

    public DateTime? Paymentdate { get; set; }

    public short? Status { get; set; }

    public virtual Ticket? Ticket { get; set; }
}
