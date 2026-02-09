using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.ViewComponents;

public class PosterCarouselViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public PosterCarouselViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? cinemaId)
    {
        var now = DateTime.Now;

        var movies = await _db.Movies
            .Include(m => m.Sessions)
                .ThenInclude(s => s.Sessionprices)
            .Include(m => m.Sessions)
                .ThenInclude(s => s.Hall)
            .Where(m => m.Sessions.Any(s =>
                s.Isactive == true &&
                s.Starttime > now &&
                (!cinemaId.HasValue || s.Hall.Cinemaid == cinemaId)))
            .ToListAsync();

        string cinemaName = "Всі кінотеатри";
        if (cinemaId.HasValue)
        {
            var cinema = await _db.Cinemas.FirstOrDefaultAsync(c => c.Id == cinemaId);
            if (cinema != null) cinemaName = cinema.Name;
        }

        var model = movies.Select(m => new PosterCarouselItemVm
        {
            MovieId = m.Id,
            Title = m.Title,
            PosterUrl = m.Posterurl,
            TrailerUrl = m.Trailerurl,
            SelectedCinemaName = cinemaName,
            SelectedCinemaId = cinemaId,
            AvailableDates = m.Sessions
                .Where(s => s.Isactive == true && (!cinemaId.HasValue || s.Hall.Cinemaid == cinemaId))
                .Select(s => s.Starttime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList(),
            TodaySessions = m.Sessions
                .Where(s => s.Isactive == true &&
                            s.Starttime.Date == now.Date &&
                            s.Starttime > now &&
                            (!cinemaId.HasValue || s.Hall.Cinemaid == cinemaId))
                .OrderBy(s => s.Starttime)
                .Select(s => new HomeSessionVm
                {
                    SessionId = s.Id,
                    StartTime = s.Starttime,
                    MinPrice = s.Sessionprices.Any() ? s.Sessionprices.Min(p => p.Price) : 0
                }).ToList()
        }).ToList();

        return View(model);
    }

}
