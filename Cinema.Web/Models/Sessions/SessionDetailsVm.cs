namespace Cinema.Web.Models.Sessions;

public class SessionDetailsVm
{
    // === Інформація про сеанс ===
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string CinemaName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;

    // === Інформація про фільм ===
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int Duration { get; set; }

    public string? AgeRestriction { get; set; }
    public DateTime? ReleaseDate { get; set; }

    // === Схема залу ===
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }

    // === Місця ===
    public List<SeatDetailsVm> Seats { get; set; } = new();
}

public class SeatDetailsVm
{
    public int SeatId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public bool IsTaken { get; set; }
}