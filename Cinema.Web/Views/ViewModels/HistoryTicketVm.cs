namespace Cinema.Web.ViewModels
{
    public class HistoryTicketVm
    {
        public int TicketId { get; set; }

        public string MovieTitle { get; set; } = "";
        public string CinemaName { get; set; } = "";
        public string CinemaCity { get; set; } = "";
        public string HallName { get; set; } = "";

        public DateTime StartTime { get; set; }
        public int Row { get; set; }
        public int Seat { get; set; }

        public decimal Price { get; set; }

        public string ActionText { get; set; } = "";
        public string BadgeClass { get; set; } = "";
        public DateTime ActionTime { get; set; }
    }
}
