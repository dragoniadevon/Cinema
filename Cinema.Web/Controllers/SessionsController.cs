using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models.Sessions;

namespace Cinema.Web.Controllers;

public class SessionsController : Controller
{
    private readonly AppDbContext _context;

    public SessionsController(AppDbContext context)
    {
        _context = context;
    }

    // ============================
    // INDEX (розклад сеансів)
    // ============================
    // GET: /Sessions
    public async Task<IActionResult> Index(int? cinemaId, DateTime? date, bool showArchived = false)
    {
        ViewBag.Cinemas = await _context.Cinemas
            .Where(c => c.Isactive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.SelectedCinema = cinemaId;
        ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
        ViewBag.ShowArchived = showArchived;

        var q = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h.Cinema)
            .Include(s => s.Tickets)
            .AsQueryable();

        if (cinemaId.HasValue)
            q = q.Where(s => s.Hall != null && s.Hall.Cinemaid == cinemaId.Value);

        if (date.HasValue)
        {
            var d = date.Value.Date;
            q = q.Where(s => s.Starttime.Date == d);
        }

        var now = DateTime.Now;
        if (!showArchived)
            q = q.Where(s => s.Starttime >= now);
        else
            q = q.Where(s => s.Starttime < now);

        var sessions = await q
            .OrderBy(s => s.Starttime)
            .ToListAsync();

        var model = sessions
            .GroupBy(s => s.Starttime.Date)
            .OrderBy(g => g.Key)
            .Select(dateGroup => new SessionsByDateVm
            {
                Date = dateGroup.Key,
                Cinemas = dateGroup
                    .GroupBy(s => s.Hall?.Cinemaid ?? 0)
                    .Select(cinemaGroup =>
                    {
                        var first = cinemaGroup.First();
                        return new SessionsByCinemaVm
                        {
                            CinemaId = first.Hall?.Cinemaid ?? 0,
                            CinemaName = first.Hall?.Cinema?.Name ?? "—",
                            Movies = cinemaGroup
                                .GroupBy(s => s.Movieid)
                                .Select(movieGroup =>
                                {
                                    var m = movieGroup.First().Movie;
                                    return new SessionsByMovieVm
                                    {
                                        MovieId = m?.Id ?? 0,
                                        Title = m?.Title ?? "—",
                                        Duration = m?.Duration ?? 0,
                                        Sessions = movieGroup.ToList()
                                    };
                                })
                                .OrderBy(x => x.Title)
                                .ToList()
                        };
                    })
                    .OrderBy(x => x.CinemaName)
                    .ToList()
            })
            .ToList();

        return View(model);
    }

    // ============================
    // CREATE (щоб кнопка "+ Додати сеанс" працювала)
    // ============================
    // GET: /Sessions/Create
    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreateSessionViewModel();
        FillCreateViewBags(model);

        
        return View(model);
    }
    // POST: /Sessions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSessionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            FillCreateViewBags(model);
            return View(model);
        }

        // 1) Перевіряємо, що зал належить вибраному кінотеатру (щоб не було багів)
        var hall = await _context.Halls
            .FirstOrDefaultAsync(h => h.Id == model.HallId && h.Cinemaid == model.CinemaId);

        if (hall == null)
        {
            ModelState.AddModelError(nameof(model.HallId), "Обраний зал не належить цьому кінотеатру.");
            FillCreateViewBags(model);
            return View(model);
        }

        // 2) Створюємо сеанс
        var session = new Session
        {
            Movieid = model.MovieId,
            Hallid = model.HallId,
            Starttime = model.StartTime
            // ⚠️ Якщо у Session є поле Format/Isactive — скажеш, додам 1:1
            // Format = model.Format,
            // Isactive = true,
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        

        return RedirectToAction(nameof(Index));
    }

    // ============================
    // DETAILS HUB (вибір сеансу для деталей)
    // ============================
    // GET: /Sessions/DetailsHub
    [HttpGet]
    public async Task<IActionResult> DetailsHub(int? cinemaId, DateTime? date)
    {
        ViewBag.Cinemas = await _context.Cinemas
            .Where(c => c.Isactive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.SelectedCinema = cinemaId;
        ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");

        var q = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h.Cinema)
            .OrderBy(s => s.Starttime)
            .AsQueryable();

        if (cinemaId.HasValue)
            q = q.Where(s => s.Hall != null && s.Hall.Cinemaid == cinemaId.Value);

        if (date.HasValue)
        {
            var d = date.Value.Date;
            q = q.Where(s => s.Starttime.Date == d);
        }

        var now = DateTime.Now;
        q = q.Where(s => s.Starttime >= now);

        var sessions = await q.ToListAsync();
        return View(sessions);
    }

    // ============================
    // DETAILS (детальна сторінка сеансу)
    // ============================
    // GET: /Sessions/Details/5
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
            .Where(s => s.Hallid == session.Hallid)
            .OrderBy(s => s.Rownumber)
            .ThenBy(s => s.Seatnumber)
            .ToListAsync();

        var takenSeatIds = await _context.Tickets
            .Where(t => t.Sessionid == id && t.Seatid != null)
            .Select(t => t.Seatid!.Value)
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

        return View(vm);
    }

    // ============================
    // CANCEL (кнопка "Скасувати")
    // ============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        // Якщо у Session є Isactive — тоді ставимо false.
        // Якщо нема — скинь модель Session, і зробимо інакше.
        session.Isactive = false;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { showArchived = true });
    }

    // ============================
    // DELETE SESSION (JSON)
    // ============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .Include(s => s.Sessionprices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
            return Json(new { success = false, message = "Сеанс не знайдено." });

        if (session.Tickets.Any())
        {
            return Json(new
            {
                success = false,
                needCancel = true,
                message = "Неможливо видалити: на сеанс уже куплені квитки. Використовуйте кнопку 'Скасувати'."
            });
        }

        try
        {
            _context.Sessionprices.RemoveRange(session.Sessionprices);
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Сеанс успішно видалено." });
        }
        catch
        {
            return Json(new { success = false, message = "Помилка бази даних при видаленні." });
        }
    }

    // ============================
    // HELPERS
    // ============================
    private void FillCreateViewBags(CreateSessionViewModel model)
    {
        ViewBag.Movies = _context.Movies.ToList();
        ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();

        ViewBag.Halls = model.CinemaId > 0
            ? _context.Halls.Where(h => h.Cinemaid == model.CinemaId && h.Isactive).ToList()
            : new List<Hall>();
    }
}
