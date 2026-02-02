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

    // ================= CREATE (GET) =================
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSessionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            FillCreateViewBags(model);
            return View(model);
        }

        var movie = await _context.Movies.FindAsync(model.MovieId);
        var hall = await _context.Halls.FindAsync(model.HallId);

        int iterations = model.RepeatDaily ? 7 : 1;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            for (int i = 0; i < iterations; i++)
            {
                var start = model.StartTime.AddDays(i);
                var end = start.AddMinutes(movie.Duration ?? 0);

                bool overlap = await _context.Sessions.AnyAsync(s =>
                    s.Hallid == hall.Id &&
                    start < s.Endtime &&
                    end > s.Starttime);

                if (overlap)
                {
                    ModelState.AddModelError("", $"Накладання на дату {start:dd.MM}");
                    await transaction.RollbackAsync();
                    FillCreateViewBags(model);
                    return View(model);
                }

                var session = new Session
                {
                    Movieid = model.MovieId,
                    Hallid = model.HallId,
                    Starttime = start,
                    Endtime = end,
                    Format = (short)model.Format,
                    Isactive = true
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                var prices = model.Prices
                    .Where(p => p.Price > 0)
                    .Select(p => new Sessionprice
                    {
                        Sessionid = session.Id,
                        Categoryid = p.PriceCategoryId,
                        Price = p.Price
                    });

                _context.Sessionprices.AddRange(prices);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Помилка при створенні");
            FillCreateViewBags(model);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult GetHallsByCinema(int cinemaId)
    {
        var halls = _context.Halls
            .Where(h => h.Cinemaid == cinemaId && h.Isactive)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Halltype
            })
            .ToList();

        return Json(halls);
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

        var hall = await _context.Halls.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == session.Hallid);

        ViewBag.Halls = hall != null
            ? await _context.Halls
                .Where(h => h.Cinemaid == hall.Cinemaid && h.Isactive)
                .ToListAsync()
            : new List<Hall>();

        ViewBag.PriceCategories = await _context.Pricecategories.ToListAsync();
    }

    // ================= EDIT (З НОВИМИ ПОЛЯМИ) =================
    public async Task<IActionResult> Edit(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Include(s => s.Sessionprices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        // Перевірка: якщо є квитки, не пускаємо на сторінку редагування
        if (session.Tickets.Any())
        {
            TempData["Error"] = "Редагування заблоковано: на цей сеанс уже продано квитки.";
            return RedirectToAction(nameof(Index));
        }

        await FillEditViewBags(session);
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Session session, int[] PriceCategoryIds, decimal[] CategoryPrices)
    {
        if (id != session.Id) return NotFound();

        var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == session.Movieid);
        if (movie == null)
        {
            ModelState.AddModelError("", "Фільм не знайдено");
            await FillEditViewBags(session);
            return View(session);
        }

        session.Endtime = session.Starttime.AddMinutes(movie.Duration ?? 0);

        bool isOverlapping = await _context.Sessions.AnyAsync(s =>
            s.Id != id &&
            s.Hallid == session.Hallid &&
            session.Starttime < s.Endtime &&
            session.Endtime > s.Starttime
        );

        if (isOverlapping)
        {
            ModelState.AddModelError("", "У цьому залі вже є інший сеанс у вибраний час");
            await FillEditViewBags(session);
            return View(session);
        }

        if (ModelState.IsValid)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Update(session);

                // Оновлення цін: видаляємо старі та додаємо нові
                var oldPrices = _context.Sessionprices.Where(sp => sp.Sessionid == id);
                _context.Sessionprices.RemoveRange(oldPrices);

                if (PriceCategoryIds != null)
                {
                    for (int i = 0; i < PriceCategoryIds.Length; i++)
                    {
                        if (CategoryPrices[i] > 0)
                        {
                            _context.Sessionprices.Add(new Sessionprice
                            {
                                Sessionid = id,
                                Categoryid = PriceCategoryIds[i],
                                Price = CategoryPrices[i]
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Помилка при збереженні.");
            }
        }

        await FillEditViewBags(session);
        return View(session);
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
            // 1. Деактивуємо сеанс
            session.Isactive = false;
            _context.Update(session);

            // 2. Деактивуємо всі квитки на цей сеанс (робимо їх недійсними)
            if (session.Tickets.Any())
            {
                foreach (var ticket in session.Tickets)
                {
                    // ticket.Isactive = false; // Зніміть коментар, якщо у квитка є поле Isactive
                    // Або якщо у вас є статус: ticket.Status = TicketStatus.Cancelled;
                }
            }

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

        return View(vm);
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