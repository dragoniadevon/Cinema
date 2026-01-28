namespace Cinema.Web.Models.Tickets;

public class BookTicketVm
{
    public int SessionId { get; set; }
    public List<SeatVm> Seats { get; set; } = new();
}