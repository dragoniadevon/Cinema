namespace Cinema.Web.Models.Tickets;

public class SeatVm
{
    public int SeatId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public bool IsTaken { get; set; }
}