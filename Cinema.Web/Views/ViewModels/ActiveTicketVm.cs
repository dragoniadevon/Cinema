namespace Cinema.Web.ViewModels
{
    public class ActiveTicketVm
    {
        public int TicketId { get; set; }

        public string MovieTitle { get; set; } = "";
        public string CinemaName { get; set; } = "";
        public string CinemaCity { get; set; } = "";
        public string HallName { get; set; } = "";

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int Row { get; set; }
        public int Seat { get; set; }

        public decimal Price { get; set; }

        public bool IsReserved { get; set; }
        public bool CanReturn { get; set; }

        public DateTime? BookingTime { get; set; }
    }
}
