namespace Cinema.Web.Models.Sessions;

public class SessionDetailsVm
{
    // === Інформація про сеанс ===
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string CinemaName { get; set; } = "—";
    public string HallName { get; set; } = "—";

    // === Інформація про фільм ===
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = "—";
    public int Duration { get; set; }

    public string? AgeRestriction { get; set; }
    public DateTime? ReleaseDate { get; set; }

    // === Схема залу ===
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }

    // === Місця ===
    public List<SeatDetailsVm> Seats { get; set; } = new();

    // ✅ НОВЕ: ціни
    public List<SessionPriceVm> Prices { get; set; } = new();
}

public class SessionPriceVm
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "—";
    public decimal Price { get; set; }
}

public class SeatDetailsVm
{
    public int SeatId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public bool IsTaken { get; set; }
}