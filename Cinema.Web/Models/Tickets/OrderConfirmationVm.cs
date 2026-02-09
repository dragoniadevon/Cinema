public class OrderConfirmationVm
{
    public List<TicketConfirmationItemVm> Tickets { get; set; } = new();
}

public class TicketConfirmationItemVm
{
    public int TicketId { get; set; }
    public string MovieTitle { get; set; } = "";
    public int Row { get; set; }
    public int Seat { get; set; }
    public decimal Price { get; set; }
}
