using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;

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
        public async Task<IActionResult> Index(int? cinemaId, string city, DateTime? date, string format = "all", string hallType = "all", string mode = "active")
        {
            mode = string.IsNullOrEmpty(mode) ? "active" : mode;
            ViewBag.CurrentMode = mode;
            ViewBag.SelectedFormat = format;
            ViewBag.SelectedHallType = hallType;

            var baseQuery = _context.Sessions
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .AsQueryable();

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

            if (format == "2D")
                baseQuery = baseQuery.Where(s => s.Format == (short)SessionFormat.TwoD);
            else if (format == "3D")
                baseQuery = baseQuery.Where(s => s.Format == (short)SessionFormat.ThreeD);

            if (hallType == "standard")
            {
                baseQuery = baseQuery.Where(s => s.Hall.Halltype == (short)HallType.Standard
                                              || s.Hall.Halltype == (short)HallType.Mixed);
            }
            else if (hallType == "vip")
            {
                baseQuery = baseQuery.Where(s => s.Hall.Halltype == (short)HallType.VIP);
            }

            ViewBag.Cities = await baseQuery
                .Select(s => s.Hall.Cinema.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var cinemasQuery = _context.Cinemas.Where(c => c.Isactive == true);
            if (!string.IsNullOrEmpty(city))
            {
                cinemasQuery = cinemasQuery.Where(c => c.City == city);
            }
            ViewBag.Cinemas = await cinemasQuery.OrderBy(c => c.Name).ToListAsync();
            ViewBag.AllAvailableDates = await baseQuery
                .Select(s => s.Starttime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var query = baseQuery
                .Include(s => s.Movie)
                .Include(s => s.Sessionprices);

            IQueryable<Session> finalQuery = query;

            DateTime activeDate = date ?? DateTime.Today;
            finalQuery = finalQuery.Where(s => s.Starttime.Date == activeDate.Date);

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
                                .Select(movieGroup => {
                                    var firstSession = movieGroup.First();
                                    return new SessionsByMovieVm
                                    {
                                        MovieId = movieGroup.Key ?? 0,
                                        Title = firstSession.Movie?.Title ?? "Фільм",
                                        Duration = firstSession.Movie?.Duration,
                                        Agerating = firstSession.Movie?.Agerating switch
                                        {
                                            AgeRating.G => "0+",
                                            AgeRating.PG => "6+",
                                            AgeRating.PG13 => "12+",
                                            AgeRating.R => "16+",
                                            AgeRating.NC17 => "18+",
                                            _ => "0+"
                                        },
                                        Sessions = movieGroup.OrderBy(s => s.Starttime).ToList()
                                    };
                                }).ToList()
                        }).ToList()
                }).ToList();

            ViewBag.Cities = await _context.Cinemas.Where(c => c.Isactive == true).Select(c => c.City).Distinct().ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive == true).ToListAsync();
            ViewBag.SelectedCinemaId = cinemaId;

            if (cinemaId.HasValue)
            {
                var cinema = await _context.Cinemas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == cinemaId);

                if (cinema != null)
                {
                    ViewBag.CinemaName = cinema.Name;
                    ViewBag.CinemaCity = cinema.City;
                    ViewBag.CinemaAddress = cinema.Address;
                }
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

                .Include(s => s.Sessionprices)
                    .ThenInclude(sp => sp.Category)

                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            var seats = await _context.Seats
                .AsNoTracking()
                .Include(s => s.Pricecategory)
                .Where(s => s.Hallid == session.Hallid)
                .OrderBy(s => s.Rownumber)
                .ThenBy(s => s.Seatnumber)
                .ToListAsync();

            var takenSeatIds = await _context.Tickets
                .Where(t => t.Sessionid == id && !t.IsReturned)
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
                Format = (SessionFormat)session.Format,

                CinemaId = session.Hall?.Cinemaid ?? 0,
                CinemaName = cinema?.Name ?? "—",
                CinemaCity = cinema?.City ?? "—",
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
                    IsBlocked = s.IsBlocked,
                    CategoryName = s.Pricecategory?.Name ?? "Стандарт"
                }).ToList()
            };
            vm.IsAdminView = false;
            return View(vm);
        }


    }
}