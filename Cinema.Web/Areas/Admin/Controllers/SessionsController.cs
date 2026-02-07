using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]/[action]/{id?}")]
public class SessionsController : Controller
{
    private readonly AppDbContext _context;
    public SessionsController(AppDbContext context)
    {
        _context = context;
    }

    private async Task FillEditViewBags(Session session)
    {
        ViewBag.Movies = await _context.Movies.OrderBy(m => m.Title).ToListAsync();

        // ПЕРЕДАЄМО ПОВНІ ОБ'ЄКТИ
        ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive).ToListAsync();

        // ДОДАЄМО МІСТА
        ViewBag.Cities = await _context.Cinemas
            .Where(c => c.Isactive)
            .Select(c => c.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var currentHall = await _context.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == session.Hallid);
        if (currentHall != null)
        {
            ViewBag.Halls = await _context.Halls
                .Where(h => h.Cinemaid == currentHall.Cinemaid && h.Isactive)
                .ToListAsync();
        }
        else
        {
            ViewBag.Halls = new List<Hall>();
        }

        ViewBag.PriceCategories = await _context.Pricecategories.ToListAsync();
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
        {
            // АРХІВ: Тільки ті, що були активні на момент завершення (тобто відбулися)
            // Навіть якщо зал зараз архівний, ми показуємо цей сеанс тут, бо він БУВ успішним.
            query = query.Where(s =>
                s.Isactive == true &&
                s.Endtime < DateTime.Now);
        }
        else if (mode == "cancelled")
        {
            // СКАСОВАНІ: 
            // 1. Сеанс було скасовано вручну АБО зал заархівували ПЕРЕД початком сеансу.
            // 2. І при цьому сеанс або майбутній, або має квитки до повернення.
            query = query.Where(s =>
                (s.Isactive == false || s.Hall.Isactive == false) &&
                (s.Endtime >= DateTime.Now || s.Tickets.Any(t => s.Isactive == false)));
            // Примітка: остання умова залежить від того, чи ви видаляєте порожні минулі сеанси.
        }
        else // active
        {
            // Тільки ті, де і сеанс активний, і зал працює.
            query = query.Where(s =>
                s.Isactive == true &&
                s.Hall.Isactive == true &&
                s.Endtime >= DateTime.Now);
        }

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
            d.IsAdminView = true;
        }

        return View(model);
    }

    // ================= CREATE =================
    public IActionResult Create()
    {
        var now = DateTime.Now;
        var model = new CreateSessionViewModel
        {
            StartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
            Prices = _context.Pricecategories
                .Select(pc => new SessionPriceInput
                {
                    PriceCategoryId = pc.Id,
                    CategoryName = pc.Name,
                    Price = 0m
                })
                .ToList()
        };

        ViewBag.Movies = _context.Movies.ToList();
        ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();
        ViewBag.Halls = new List<Hall>();

        ViewBag.Cities = _context.Cinemas
            .Where(c => c.Isactive)
            .Select(c => c.City)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult GetHallsByCinema(int cinemaId)
    {
        var halls = _context.Halls
            .Where(h => h.Cinemaid == cinemaId && h.Isactive)
            .Select(h => new { h.Id, h.Name, h.Halltype })
            .ToList();
        return Json(halls);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSessionViewModel model)
    {
        if (!ModelState.IsValid) { FillCreateViewBags(model); return View(model); }

        var movie = await _context.Movies.FindAsync(model.MovieId);
        var hall = await _context.Halls.FindAsync(model.HallId);

        if (movie == null)
        {
            ModelState.AddModelError("", "Фільм не знайдено.");
            FillCreateViewBags(model); return View(model);
        }

        if (movie.Releasedate.HasValue)
        {
            DateTime releaseDateTime = movie.Releasedate.Value.ToDateTime(TimeOnly.MinValue);

            if (model.StartTime < releaseDateTime)
            {
                ModelState.AddModelError("StartTime",
                    $"Не вдалося створити сеанс. Дата релізу цього фільму: {releaseDateTime:dd.MM.yyyy}. Сеанси раніше цієї дати заборонені.");
                FillCreateViewBags(model);
                return View(model);
            }
        }

        int iterations = model.RepeatDaily ? 7 : 1;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            for (int i = 0; i < iterations; i++)
            {
                DateTime currentStart = model.StartTime.AddDays(i);
                DateTime currentEnd = currentStart.AddMinutes(movie.Duration ?? 0);

                bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                    s.Hallid == hall.Id && currentStart < s.Endtime && currentEnd > s.Starttime);

                if (isOverlapping)
                {
                    ModelState.AddModelError("", $"Накладання на дату {currentStart:dd.MM}. Створення зупинено.");
                    await transaction.RollbackAsync();
                    FillCreateViewBags(model); return View(model);
                }

                var session = new Session
                {
                    Movieid = model.MovieId,
                    Hallid = model.HallId,
                    Starttime = currentStart,
                    Endtime = currentEnd,
                    Format = (short)model.Format,
                    Isactive = true
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                var prices = model.Prices.Where(p => p.Price > 0).Select(p => new Sessionprice
                {
                    Sessionid = session.Id,
                    Categoryid = p.PriceCategoryId,
                    Price = p.Price
                }).ToList();

                if (prices.Any()) _context.Sessionprices.AddRange(prices);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Помилка при масовому створенні.");
            FillCreateViewBags(model); return View(model);
        }
    }

    // ================= EDIT =================
    public async Task<IActionResult> Edit(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .Include(s => s.Sessionprices)
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        var allCategories = await _context.Pricecategories.ToListAsync();

        var model = new EditSessionViewModel
        {
            Id = session.Id,
            MovieId = session.Movieid ?? 0,
            CinemaId = session.Hall?.Cinemaid ?? 0, // Не забудьте це!
            HallId = session.Hallid ?? 0,
            StartTime = session.Starttime,
            Format = (SessionFormat)session.Format,

            // Ініціалізуємо Prices для відображення в View
            Prices = allCategories.Select(c => new SessionPriceInput
            {
                PriceCategoryId = c.Id,
                CategoryName = c.Name,
                Price = session.Sessionprices.FirstOrDefault(sp => sp.Categoryid == c.Id)?.Price ?? 0
            }).ToList()
        };

        await FillEditViewBags(session);
        return View(model);
    }

    // GET: Admin/Sessions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditSessionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Створюємо тимчасовий об'єкт для FillEditViewBags
            var tempSession = new Session { Hallid = model.HallId };
            await FillEditViewBags(tempSession);
            return View(model);
        }

        var session = await _context.Sessions
            .Include(s => s.Sessionprices)
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (session == null) return NotFound();

        var movie = await _context.Movies.FindAsync(model.MovieId);
        if (movie == null)
        {
            ModelState.AddModelError("", "Фільм не знайдено");
            await FillEditViewBags(session);
            return View(model);
        }

        if (movie.Releasedate.HasValue)
        {
            DateTime releaseDateTime = movie.Releasedate.Value.ToDateTime(TimeOnly.MinValue);
            if (model.StartTime < releaseDateTime)
            {
                ModelState.AddModelError("StartTime",
                    $"Дата сеансу не може бути ранішою за дату релізу фільму ({releaseDateTime:dd.MM.yyyy}).");

                await FillEditViewBags(session);
                return View(model);
            }
        }

        DateTime endTime = model.StartTime.AddMinutes(movie.Duration ?? 0);

        bool overlap = await _context.Sessions.AnyAsync(s =>
            s.Id != model.Id &&
            s.Hallid == model.HallId &&
            model.StartTime < s.Endtime &&
            endTime > s.Starttime);

        if (overlap)
        {
            ModelState.AddModelError("", "У цьому залі вже є інший сеанс у вибраний час");
            await FillEditViewBags(session);
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            session.Movieid = model.MovieId;
            session.Hallid = model.HallId;
            session.Starttime = model.StartTime;
            session.Endtime = model.StartTime.AddMinutes(movie.Duration ?? 0);
            session.Format = (short)model.Format;

            // Оновлюємо ціни через нову колекцію Prices
            _context.Sessionprices.RemoveRange(session.Sessionprices);

            if (model.Prices != null)
            {
                foreach (var p in model.Prices.Where(x => x.Price > 0))
                {
                    _context.Sessionprices.Add(new Sessionprice
                    {
                        Sessionid = session.Id,
                        Categoryid = p.PriceCategoryId,
                        Price = p.Price
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Помилка при збереженні");
            await FillEditViewBags(session);
            return View(model);
        }
    }


    // ================= CANCEL =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null) return NotFound();

        session.Isactive = false;
        await _context.SaveChangesAsync();

        // ТАКОЖ ТУТ
        return RedirectToAction(nameof(Index));
    }

    // ================= DELETE =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _context.Sessions.Include(s => s.Sessionprices).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        _context.Sessionprices.RemoveRange(session.Sessionprices);
        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        // ЗАМІСТЬ return Json(...) використовуємо:
        return RedirectToAction(nameof(Index));
    }

    private void FillCreateViewBags(CreateSessionViewModel model)
    {
        ViewBag.Movies = _context.Movies.ToList();
        ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();
        ViewBag.Halls = model.CinemaId > 0
            ? _context.Halls.Where(h => h.Cinemaid == model.CinemaId && h.Isactive).ToList()
            : new List<Hall>();
    }

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
            .ToListAsync();

        var takenSeatIds = await _context.Tickets
            .Where(t => t.Sessionid == id)
            .Select(t => t.Seatid)
            .ToListAsync();

        var vm = new SessionDetailsVm
        {
            SessionId = session.Id,
            StartTime = session.Starttime,
            EndTime = session.Endtime,

            CinemaName = session.Hall?.Cinema?.Name ?? "—",
            HallName = session.Hall?.Name ?? "—",

            MovieId = session.Movie?.Id ?? 0,
            MovieTitle = session.Movie?.Title ?? "—",
            Duration = session.Movie?.Duration ?? 0,
            Posterurl = session.Movie?.Posterurl,

            AgeRestriction = session.Movie?.Agerating switch
            {
                AgeRating.G => "0+",
                AgeRating.PG => "6+",
                AgeRating.PG13 => "12+",
                AgeRating.R => "16+",
                AgeRating.NC17 => "18+",
                _ => "0+"
            },

            Rows = session.Hall?.Rows ?? 0,
            SeatsPerRow = session.Hall?.Seatsperrow ?? 0,

            Prices = session.Sessionprices.Select(sp => new SessionPriceVm
            {
                CategoryId = sp.Categoryid ?? 0,
                CategoryName = sp.Category?.Name ?? "—",
                Price = sp.Price
            }).ToList(),

            Seats = seats.Select(s => new SeatDetailsVm
            {
                SeatId = s.Id,
                Row = s.Rownumber ?? 0,
                Number = s.Seatnumber ?? 0,
                IsTaken = takenSeatIds.Contains(s.Id),
                CategoryName = s.Pricecategory?.Name ?? "Стандарт"
            }).ToList(),

            IsAdminView = true
        };

        return View(vm);
    }
}