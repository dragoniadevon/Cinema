using Cinema.Infrastructure.Entities.Enums;
public class OrderConfirmationVm
{
    public int SessionId { get; set; }
    public List<TicketConfirmationItemVm> Tickets { get; set; } = new();
}

public class TicketConfirmationItemVm
{
    public int TicketId { get; set; }

    public string MovieTitle { get; set; } = "";
    public string CinemaCity { get; set; } = "";
    public string CinemaName { get; set; } = "";
    public string HallName { get; set; } = "";
    public SessionFormat? Format { get; set; }

    public DateTime SessionStart { get; set; }

    public int Row { get; set; }
    public int Seat { get; set; }

    public decimal Price { get; set; }
}

