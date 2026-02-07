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

            var baseQuery = _context.Sessions
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .AsQueryable();

            // Фільтри режиму
            if (mode == "past")
                baseQuery = baseQuery.Where(s => s.Isactive == true && s.Endtime < DateTime.Now);
            else if (mode == "cancelled")
                baseQuery = baseQuery.Where(s => s.Isactive == false || (s.Hall != null && s.Hall.Isactive == false));
            else
                baseQuery = baseQuery.Where(s => s.Isactive == true && (s.Hall != null && s.Hall.Isactive == true) && s.Endtime >= DateTime.Now);

            if (!string.IsNullOrEmpty(city))
                baseQuery = baseQuery.Where(s => s.Hall.Cinema.City == city);

            if (cinemaId.HasValue)
                baseQuery = baseQuery.Where(s => s.Hall.Cinemaid == cinemaId.Value);

            // Список дат для кнопок
            ViewBag.AllAvailableDates = await baseQuery
                .Select(s => s.Starttime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var query = baseQuery
                .Include(s => s.Movie)
                .Include(s => s.Sessionprices);

            IQueryable<Session> finalQuery = query;

            // ВИЗНАЧАЄМО ОБРАНУ ДАТУ (якщо null — то сьогодні)
            DateTime activeDate = date ?? DateTime.Today;
            finalQuery = finalQuery.Where(s => s.Starttime.Date == activeDate.Date);

            // Передаємо рядок для порівняння у View
            ViewBag.SelectedDate = activeDate.ToString("yyyy-MM-dd");

            var sessions = await finalQuery.OrderBy(s => s.Starttime).ToListAsync();

            var model = sessions
                .GroupBy(s => s.Starttime.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select(dateGroup => new SessionsByDateVm
                {
                    Date = dateGroup.Key,
                    Cinemas = dateGroup
                        .GroupBy(s => s.Hall.Cinemaid)
                        .Select(cinemaGroup => new SessionsByCinemaVm
                        {
                            CinemaId = cinemaGroup.Key ?? 0,
                            CinemaName = cinemaGroup.First().Hall?.Cinema?.Name ?? "Кінотеатр",
                            Movies = cinemaGroup
                                .GroupBy(s => s.Movieid)
                                .Select(movieGroup => new SessionsByMovieVm
                                {
                                    MovieId = movieGroup.Key ?? 0,
                                    Title = movieGroup.First().Movie?.Title ?? "Фільм",
                                    Duration = movieGroup.First().Movie?.Duration,
                                    Sessions = movieGroup.OrderBy(s => s.Starttime).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            ViewBag.Cities = await _context.Cinemas.Where(c => c.Isactive == true).Select(c => c.City).Distinct().ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive == true).ToListAsync();
            ViewBag.SelectedCinemaId = cinemaId;

            return View(model);
        }
        // ============================
        // DETAILS (детальна сторінка сеансу)
        // ============================
        public async Task<IActionResult> Details(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Movie) // Обов'язково підключаємо дані про фільм
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
                .AsNoTracking()
                .Include(s => s.Pricecategory)
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
                Posterurl = session.Movie.Posterurl,
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
                    IsTaken = takenSeatIds.Contains(s.Id),
                    CategoryName = s.Pricecategory?.Name ?? "Стандарт"
                }).ToList()
            };
            vm.IsAdminView = false;
            return View(vm);
        }


    }
}