using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models.Sessions;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class SessionsController : Controller
{
    private readonly AppDbContext _context;
    public SessionsController(AppDbContext context)
    {
        _context = context;
    }

    private async Task FillEditViewBags(Session session)
    {
        ViewBag.Movies = await _context.Movies.ToListAsync();
        ViewBag.Cinemas = _context.Cinemas
            .Where(c => c.Isactive)
            .Select(c => new
            {
                Id = c.Id,
                DisplayName = $"{c.Name} ({c.City})"
            })
            .ToList();

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
            d.IsAdminView = true;
        }

        return View("~/Views/Sessions/Index.cshtml", model);
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
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Include(s => s.Sessionprices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        if (session.Tickets.Any())
        {
            TempData["Error"] = "Редагування заблоковано: на цей сеанс уже продано квитки.";
            return RedirectToAction(nameof(Index));
        }

        var allCategories = await _context.Pricecategories.ToListAsync();
        var sessionPrices = session.Sessionprices.ToList();

        var model = new EditSessionViewModel
        {
            Id = session.Id,
            MovieId = session.Movieid ?? 0,
            HallId = session.Hallid ?? 0,
            StartTime = session.Starttime,
            Format = (SessionFormat)session.Format,

            PriceCategoryIds = allCategories.Select(c => c.Id).ToArray(),
            CategoryPrices = allCategories.Select(c =>
                sessionPrices.FirstOrDefault(sp => sp.Categoryid == c.Id)?.Price ?? 0m
            ).ToArray()
        };


        await FillEditViewBags(session);
        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditSessionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var sessionForBags = await _context.Sessions
                .Include(s => s.Hall)
                .FirstAsync(s => s.Id == model.Id);

            await FillEditViewBags(sessionForBags);
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
            session.Endtime = endTime;
            session.Format = (short)model.Format;

            _context.Sessionprices.RemoveRange(session.Sessionprices);

            for (int i = 0; i < model.PriceCategoryIds.Length; i++)
            {
                if (model.CategoryPrices[i] > 0)
                {
                    _context.Sessionprices.Add(new Sessionprice
                    {
                        Sessionid = session.Id,
                        Categoryid = model.PriceCategoryIds[i],
                        Price = model.CategoryPrices[i]
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
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return Json(new { success = false, message = "Сеанс не знайдено." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            session.Isactive = false;
            _context.Update(session);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, message = "Сеанс та всі квитки скасовані." });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = "Помилка при скасуванні сеансу." });
        }
    }

    // ================= DELETE =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .Include(s => s.Sessionprices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return Json(new { success = false, message = "Сеанс не знайдено." });

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
        catch (Exception)
        {
            return Json(new { success = false, message = "Помилка бази даних при видаленні." });
        }
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
            .Where(s => s.Hallid == session.Hallid)
            .OrderBy(s => s.Rownumber)
            .ThenBy(s => s.Seatnumber)
            .ToListAsync();

        var takenSeatIds = await _context.Tickets
            .Where(t => t.Sessionid == id && t.Seatid != null)
            .Select(t => t.Seatid!.Value)
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
                IsTaken = takenSeatIds.Contains(s.Id)
            }).ToList(),

            IsAdminView = true
        };

        return View("~/Views/Sessions/Details.cshtml", vm);
    }

}
