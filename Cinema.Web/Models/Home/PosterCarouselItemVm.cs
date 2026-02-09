
public class PosterCarouselItemVm
{
    public int MovieId { get; set; }
    public string Title { get; set; } = null!;
    public string PosterUrl { get; set; } = null!;
    public string? TrailerUrl { get; set; }
    public List<DateTime> AvailableDates { get; set; } = new();
    public List<HomeSessionVm> TodaySessions { get; set; } = new();
    public string SelectedCinemaName { get; set; }
    public int? SelectedCinemaId { get; set; }
}


public class HomeSessionVm
{
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal MinPrice { get; set; }
}