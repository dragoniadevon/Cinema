using System;
using System.Collections.Generic;

namespace Cinema.Web.Models.Payments;

public class PaymentVm
{
    public List<TicketPaymentVm> Tickets { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public int MinutesLeft { get; set; }
}

public class TicketPaymentVm
{
    public int TicketId { get; set; }

    public string MovieTitle { get; set; } = "";

    public int Row { get; set; }

    public int SeatNumber { get; set; }

    public decimal Price { get; set; }
}
