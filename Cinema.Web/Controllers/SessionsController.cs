using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models.Sessions;

namespace Cinema.Web.Controllers
{
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int? cinemaId, string city, DateTime? date, string mode = "active")
        {
            mode = string.IsNullOrEmpty(mode) ? "active" : mode;
            ViewBag.CurrentMode = mode;

            var query = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Tickets)
                .AsQueryable();

            if (mode == "past")
                query = query.Where(s => s.Isactive == true && s.Endtime < DateTime.Now);
            else if (mode == "cancelled")
                query = query.Where(s => s.Isactive == false || s.Hall.Isactive == false);
            else
                query = query.Where(s => s.Isactive == true && s.Hall.Isactive == true && s.Endtime >= DateTime.Now);

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(s => s.Hall.Cinema.City == city);
            }

            if (cinemaId.HasValue)
            {
                query = query.Where(s => s.Hall.Cinemaid == cinemaId.Value);
            }

            if (date.HasValue) query = query.Where(s => s.Starttime.Date == date.Value.Date);

            var sessions = await query.OrderByDescending(s => s.Starttime).ToListAsync();

            var model = sessions
                .GroupBy(s => s.Starttime.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select(dateGroup => new SessionsByDateVm
                {
                    Date = dateGroup.Key,
                    Cinemas = dateGroup
                        .Where(s => s.Hall?.Cinema != null)
                        .GroupBy(s => s.Hall.Cinemaid)
                        .OrderBy(g => g.First().Hall.Cinema.Name)
                        .Select(cinemaGroup => new SessionsByCinemaVm
                        {
                            CinemaId = cinemaGroup.Key ?? 0,
                            CinemaName = cinemaGroup.First().Hall.Cinema.Name,
                            Movies = cinemaGroup
                                .Where(s => s.Movie != null)
                                .GroupBy(s => s.Movieid)
                                .OrderBy(g => g.First().Movie.Title)
                                .Select(movieGroup => new SessionsByMovieVm
                                {
                                    MovieId = movieGroup.Key ?? 0,
                                    Title = movieGroup.First().Movie.Title,
                                    Duration = movieGroup.First().Movie.Duration,
                                    Sessions = movieGroup.OrderBy(s => s.Starttime).ToList()
                                }).ToList()
                        }).ToList()
                })
                .Where(d => d.Cinemas.Any())
                .ToList();

            ViewBag.Cities = await _context.Cinemas
                .Where(c => c.Isactive == true)
                .Select(c => c.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive == true).ToListAsync();

            ViewBag.SelectedCity = city;
            ViewBag.SelectedCinema = cinemaId;
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");

            foreach (var d in model)
            {
                d.IsAdminView = false;
            }

            return View(model);
        }
        // ============================
        // DETAILS (детальна сторінка сеансу)
        // ============================
        public async Task<IActionResult> Details(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.Cinema)

                // ✅ НОВЕ: підтягуємо ціни + категорії
                .Include(s => s.Sessionprices)
                    .ThenInclude(sp => sp.Category)

                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            // місця в залі
            var seats = await _context.Seats
                .Where(s => s.Hallid == session.Hallid)
                .OrderBy(s => s.Rownumber)
                .ThenBy(s => s.Seatnumber)
                .ToListAsync();

            // зайняті місця
            var takenSeatIds = await _context.Tickets
                .Where(t => t.Sessionid == id)
                .Select(t => t.Seatid)
                .ToListAsync();

            var movie = session.Movie;
            var hall = session.Hall;
            var cinema = hall?.Cinema;

            var duration = movie?.Duration ?? 0;
            var start = session.Starttime;
            var end = start.AddMinutes(duration);

            var vm = new SessionDetailsVm
            {
                SessionId = session.Id,
                StartTime = start,
                EndTime = end,

                CinemaName = cinema?.Name ?? "—",
                HallName = hall?.Name ?? "—",

                MovieId = movie?.Id ?? 0,
                MovieTitle = movie?.Title ?? "—",
                Duration = duration,

                AgeRestriction = movie?.Agerating switch
                {
                    null => null,
                    AgeRating.G => "0+",
                    AgeRating.PG => "6+",
                    AgeRating.PG13 => "12+",
                    AgeRating.R => "16+",
                    AgeRating.NC17 => "18+",
                    _ => movie!.Agerating.ToString()
                },

                ReleaseDate = movie?.Releasedate.HasValue == true
                    ? movie.Releasedate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                Rows = hall?.Rows ?? 0,
                SeatsPerRow = hall?.Seatsperrow ?? 0,

                // ✅ НОВЕ: ціни квитків в деталях
                Prices = session.Sessionprices?
                    .OrderBy(sp => sp.Categoryid)
                    .Select(sp => new SessionPriceVm
                    {
                        CategoryId = sp.Categoryid ?? 0,
                        CategoryName = sp.Category?.Name ?? "—",
                        Price = sp.Price
                    })
                    .ToList() ?? new List<SessionPriceVm>(),

                Seats = seats.Select(s => new SeatDetailsVm
                {
                    SeatId = s.Id,
                    Row = s.Rownumber ?? 0,
                    Number = s.Seatnumber ?? 0,
                    IsTaken = takenSeatIds.Contains(s.Id)
                }).ToList()
            };
            vm.IsAdminView = false;
            return View(vm);
        }


    }
}