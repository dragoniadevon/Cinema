using System;
using System.Collections.Generic;

namespace Cinema.Web.Models.Payments;

public class PaymentVm
{
    public List<TicketPaymentVm> Tickets { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public DateTime ExpiryTime { get; set; }
}

public class TicketPaymentVm
{
    public int TicketId { get; set; }

    public string MovieTitle { get; set; } = "";

    public string CinemaCity { get; set; } = "";

    public string CinemaName { get; set; } = "";
    public string HallName { get; set; } = "";

    public DateTime SessionStart { get; set; }

    public int Row { get; set; }
    public int SeatNumber { get; set; }

    public decimal Price { get; set; }
}

