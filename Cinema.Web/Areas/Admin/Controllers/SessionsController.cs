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

        ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive).ToListAsync();

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
    public async Task<IActionResult> Index(int? cinemaId, string city, DateTime? date, string mode = "active", int page = 1)
    {
        mode = string.IsNullOrEmpty(mode) ? "active" : mode;
        ViewBag.CurrentMode = mode;
        var now = DateTime.Now;

        var query = _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .Include(s => s.Tickets).ThenInclude(t => t.Payment)
            .AsQueryable();

        // 1. Базова фільтрація за режимами
        if (mode == "past")
        {
            var pastQuery = query.Where(s =>
                s.Isactive == true &&
                s.Endtime < now &&
                s.Tickets.Any(t => t.Status == (short)TicketStatus.Paid));

            if (!date.HasValue)
            {
                var limitDate = now.AddDays(-30);
                pastQuery = pastQuery.Where(s => s.Starttime >= limitDate);
                ViewBag.ArchiveInfo = "Показано сеанси за останні 30 днів. Для пошуку старіших використовуйте фільтр дати.";
            }
            query = pastQuery;
        }
        else if (mode == "cancelled")
        {
            query = query.Where(s =>
                s.Isactive == false ||
                s.Hall.Isactive == false ||
                (s.Starttime < now.AddMinutes(-20) && !s.Tickets.Any(t => t.Status == (short)TicketStatus.Paid)) ||
                (s.Endtime < now && !s.Tickets.Any(t => t.Status == (short)TicketStatus.Paid))
            );
        }
        else // mode == "active"
        {
            query = query.Where(s =>
                s.Isactive == true &&
                s.Hall.Isactive == true &&
                s.Endtime >= now &&
                !(s.Starttime < now.AddMinutes(-20) && !s.Tickets.Any(t => t.Status == (short)TicketStatus.Paid))
            );
        }

        // 2. Фільтри за містом/кінотеатром/датою
        if (!string.IsNullOrEmpty(city)) query = query.Where(s => s.Hall.Cinema.City == city);
        if (cinemaId.HasValue) query = query.Where(s => s.Hall.Cinemaid == cinemaId.Value);
        if (date.HasValue) query = query.Where(s => s.Starttime.Date == date.Value.Date);

        var sessions = await query.ToListAsync();

        // 3. Групування та СОРТУВАННЯ (виносимо логіку з View)
        var dateGroups = sessions.GroupBy(s => s.Starttime.Date).ToList();

        IEnumerable<IGrouping<DateTime, Session>> sortedGroups;
        if (mode == "active")
        {
            sortedGroups = dateGroups.OrderBy(g => g.Key);
        }
        else if (mode == "cancelled")
        {
            // Майбутні скасовані (ближчі до сьогодні) + Минулі скасовані (від нових до старих)
            var future = dateGroups.Where(g => g.Key >= now.Date).OrderBy(g => g.Key);
            var past = dateGroups.Where(g => g.Key < now.Date).OrderByDescending(g => g.Key);
            sortedGroups = future.Concat(past);
        }
        else
        { // past
            sortedGroups = dateGroups.OrderByDescending(g => g.Key);
        }

        // 4. Пагінація
        int pageSize = 5;
        var pagedGroups = sortedGroups.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // 5. Формування моделі
        var model = pagedGroups.Select(dateGroup => new SessionsByDateVm
        {
            Date = dateGroup.Key,
            Cinemas = dateGroup
                .GroupBy(s => s.Hall.Cinemaid)
                .OrderBy(g => g.First().Hall.Cinema.Name)
                .Select(cinemaGroup => new SessionsByCinemaVm
                {
                    CinemaId = cinemaGroup.Key ?? 0,
                    CinemaName = cinemaGroup.First().Hall.Cinema.Name,
                    Movies = cinemaGroup
                        .GroupBy(s => s.Movieid)
                        .OrderBy(g => g.First().Movie.Title)
                        .Select(movieGroup => new SessionsByMovieVm
                        {
                            MovieId = movieGroup.Key ?? 0,
                            Title = movieGroup.First().Movie.Title,
                            Duration = movieGroup.First().Movie.Duration,
                            Agerating = movieGroup.First().Movie.Agerating switch
                            {
                                AgeRating.G => "0+",
                                AgeRating.PG => "6+",
                                AgeRating.PG13 => "12+",
                                AgeRating.R => "16+",
                                AgeRating.NC17 => "18+",
                                _ => "0+"
                            },
                            Format = (SessionFormat)movieGroup.First().Format == SessionFormat.ThreeD ? "3D" : "2D",
                            Sessions = movieGroup.OrderBy(s => s.Starttime).Select(s => {
                                var tks = s.Tickets.ToList();
                                s.PaidCount = tks.Count(t => t.Status == (short)TicketStatus.Paid);
                                s.RefundedCount = tks.Count(t => t.Status == (short)TicketStatus.Cancelled && t.Payment?.Status == (short)PaymentStatus.Paid);
                                s.CancelledResCount = tks.Count(t => t.Status == (short)TicketStatus.Cancelled && t.Payment?.Status != (short)PaymentStatus.Paid);
                                s.ActiveTicketsCount = tks.Count(t => t.Status != (short)TicketStatus.Cancelled);
                                return s;
                            }).ToList()
                        }).ToList()
                }).ToList()
        }).ToList();

        // 6. ViewBags
        ViewBag.Cities = await _context.Cinemas.Where(c => c.Isactive).Select(c => c.City).Distinct().OrderBy(c => c).ToListAsync();
        ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive).ToListAsync();
        ViewBag.Page = page;
        ViewBag.HasNextPage = dateGroups.Count > page * pageSize;
        ViewBag.SelectedCity = city;
        ViewBag.SelectedCinema = cinemaId;
        ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");

        foreach (var d in model) d.IsAdminView = true;
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
        if (!ModelState.IsValid)
        {
            FillCreateViewBags(model);
            return View(model);
        }

        var movie = await _context.Movies.FindAsync(model.MovieId);
        var hall = await _context.Halls.FindAsync(model.HallId);

        if (movie == null || hall == null)
        {
            ModelState.AddModelError("", "Фільм або зал не знайдено.");
            FillCreateViewBags(model);
            return View(model);
        }

        if (movie.Releasedate.HasValue)
        {
            DateTime releaseDateTime = movie.Releasedate.Value.ToDateTime(TimeOnly.MinValue);

            if (model.StartTime < releaseDateTime)
            {
                ModelState.AddModelError("StartTime",
                    $"Увага! Реліз фільму заплановано на {releaseDateTime:dd.MM.yyyy}. Створення сеансів до цієї дати неможливе.");

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
                    s.Hallid == hall.Id && s.Isactive == true &&
                    currentStart < s.Endtime && currentEnd > s.Starttime);

                if (isOverlapping)
                {
                    ModelState.AddModelError("StartTime", $"Конфлікт розкладу: у залі {hall.Name} вже є сеанс на дату {currentStart:dd.MM HH:mm}.");
                    await transaction.RollbackAsync();
                    FillCreateViewBags(model);
                    return View(model);
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

                var prices = model.Prices
                    .Where(p => p.Price > 0)
                    .Select(p => new Sessionprice
                    {
                        Sessionid = session.Id,
                        Categoryid = p.PriceCategoryId,
                        Price = p.Price
                    }).ToList();

                if (prices.Any()) _context.Sessionprices.AddRange(prices);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Сеанс(и) успішно створено!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Виникла помилка під час збереження даних у базу.");
            FillCreateViewBags(model);
            return View(model);
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
            CinemaId = session.Hall?.Cinemaid ?? 0,
            HallId = session.Hallid ?? 0,
            StartTime = session.Starttime,
            Format = (SessionFormat)session.Format,

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
    // ================= CANCEL (Скасування сеансу адміном) =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        // Завантажуємо сеанс разом із квитками
        var session = await _context.Sessions
            .Include(s => s.Tickets)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        // 1. Робимо сеанс неактивним
        session.Isactive = false;

        // 2. Скасовуємо всі квитки на цей сеанс (і оплати, і броні)
        if (session.Tickets.Any())
        {
            foreach (var ticket in session.Tickets.Where(t => t.Status != (short)TicketStatus.Cancelled))
            {
                ticket.Status = (short)TicketStatus.Cancelled;
                ticket.Bookingtime = DateTime.Now; // Фіксуємо час скасування
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Сеанс скасовано, всі квитки повернуто клієнтам.";

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

        return RedirectToAction(nameof(Index));
    }

    private void FillCreateViewBags(CreateSessionViewModel model)
    {
        ViewBag.Movies = _context.Movies.OrderBy(m => m.Title).ToList();

        ViewBag.Cities = _context.Cinemas
            .Where(c => c.Isactive)
            .Select(c => c.City)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();

        if (model.CinemaId > 0)
        {
            ViewBag.Halls = _context.Halls
                .Where(h => h.Cinemaid == model.CinemaId && h.Isactive)
                .ToList();
        }
        else
        {
            ViewBag.Halls = new List<Hall>();
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var session = await _context.Sessions
        .Include(s => s.Movie)
        .Include(s => s.Hall).ThenInclude(h => h.Cinema)
        .Include(s => s.Tickets).ThenInclude(t => t.User)
        .Include(s => s.Sessionprices).ThenInclude(sp => sp.Category)
        .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        var seats = await _context.Seats
            .AsNoTracking()
            .Include(s => s.Pricecategory)
            .Where(s => s.Hallid == session.Hallid)
            .OrderBy(s => s.Rownumber).ThenBy(s => s.Seatnumber)
            .ToListAsync();

        var now = DateTime.Now;
        var takenSeatIds = await _context.Tickets
            .Where(t =>
                t.Sessionid == id &&
                (
                    t.Status == (short)TicketStatus.Paid ||
                    (t.Status == (short)TicketStatus.Reserved &&
                     now <= t.Bookingtime.AddMinutes(10))
                )
            )
            .Select(t => t.Seatid)
            .ToListAsync();


        var vm = new SessionDetailsVm
        {
            SessionId = session.Id,
            MovieTitle = session.Movie?.Title ?? "—",
            Duration = session.Movie?.Duration ?? 0,
            Format = (SessionFormat)session.Format,

            CinemaName = session.Hall?.Cinema?.Name ?? "—",
            CinemaCity = session.Hall?.Cinema?.City ?? "—",
            HallName = session.Hall?.Name ?? "—",
            StartTime = session.Starttime,
            EndTime = session.Endtime,
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

            Seats = seats.Select(s =>
            {
                var now = DateTime.Now;

                // 1. Шукаємо АКТИВНИЙ квиток (оплачений або живе бронювання)
                var activeTicket = session.Tickets.FirstOrDefault(t =>
                    t.Seatid == s.Id &&
                    (t.Status == (short)TicketStatus.Paid ||
                    (t.Status == (short)TicketStatus.Reserved && now <= t.Bookingtime.AddMinutes(10))));

                // 2. Шукаємо ОСТАННІЙ скасований квиток на це місце (для історії повернень)
                var lastCancelled = session.Tickets
    .Where(t => t.Seatid == s.Id && t.Status == (short)TicketStatus.Cancelled)
    .OrderByDescending(t => t.Bookingtime)
    .FirstOrDefault();

                // Визначаємо тип скасованого квитка
                string typeLabel = lastCancelled?.Price > 0 ? "Оплата" : "Бронь";


                return new SeatDetailsVm
                {
                    SeatId = s.Id,
                    Row = s.Rownumber ?? 0,
                    Number = s.Seatnumber ?? 0,
                    IsTaken = activeTicket != null,
                    IsBlocked = s.IsBlocked,
                    CategoryName = s.Pricecategory?.Name ?? "Стандарт",

                    // Дані для Offcanvas панелі
                    Price = activeTicket?.Price ?? 0,
                    CustomerName = activeTicket != null ? $"{activeTicket.User?.FirstName} {activeTicket.User?.LastName}" : null,
                    CustomerEmail = activeTicket?.User?.Email,
                    PurchaseDate = activeTicket?.Bookingtime,

                    // Статус текстом
                    StatusInfo = activeTicket?.Status switch
                    {
                        (short)TicketStatus.Paid => "Оплачено",
                        (short)TicketStatus.Reserved => "Бронювання",
                        _ => "Вільне"
                    },

                    // Інфо про повернення (якщо місце вільне, але був скасований квиток)
                    RefundInfo = lastCancelled != null
                    ? $"Попереднє замовлення ({typeLabel}): {lastCancelled.User?.FirstName} (Скасовано {lastCancelled.Bookingtime:dd.MM HH:mm})"
                    : null
                };
            }).ToList(),
            IsAdminView = true
        };

        return View(vm);
    }

    // ==========================================
    // CANCEL TICKET: Видаляє квиток та звільняє місце
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelTicket(int seatId, int sessionId)
    {
        try
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t =>
                    t.Seatid == seatId &&
                    t.Sessionid == sessionId &&
                    (t.Status == (short)TicketStatus.Paid || t.Status == (short)TicketStatus.Reserved));

            if (ticket == null)
                return Json(new { success = false, message = "Активний квиток не знайдено." });

            ticket.Status = (short)TicketStatus.Cancelled;
            // ✅ Оновлюємо час на момент натискання кнопки адміном
            ticket.Bookingtime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Помилка сервера: " + ex.Message });
        }
    }

    // ==========================================
    // TOGGLE BLOCK: Блокує/розблокує місце (технічна несправність)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlockSeat(int seatId)
    {
        var seat = await _context.Seats.FindAsync(seatId);
        if (seat == null) return NotFound();

        seat.IsBlocked = !seat.IsBlocked;

        await _context.SaveChangesAsync();
        return Json(new { success = true, isBlocked = seat.IsBlocked });
    }
}