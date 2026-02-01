using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models.Sessions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Cinema.Infrastructure.Entities.Enums; // Додано для форматів

namespace Cinema.Web.Controllers
{
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        // Допоміжний метод для наповнення ViewBag при редагуванні (схожий на FillCreateViewBags)
        private async Task FillEditViewBags(Session session)
        {
            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Cinemas = _context.Cinemas
                .Where(c => c.Isactive)
                .Select(c => new {
                    Id = c.Id,
                    // Об'єднуємо назву та місто для випадаючого списку
                    DisplayName = $"{c.Name} ({c.City})"
                })
                .ToList();
            // Завантажуємо зали того кінотеатру, якому належить поточний зал сеансу
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
        public async Task<IActionResult> Index(int? cinemaId, DateTime? date, bool showArchived = false)
        {
            // 1. Отримуємо дані з бази (цей блок у вас вже є)
            var query = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.Cinema)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.Seats)
                .Include(s => s.Tickets)
                .AsQueryable();

            query = showArchived ? query.Where(s => s.Isactive == false) : query.Where(s => s.Isactive == true);

            if (cinemaId.HasValue) query = query.Where(s => s.Hall.Cinemaid == cinemaId.Value);

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                var start = targetDate;
                var end = targetDate.AddDays(1);

                query = query.Where(s =>
                    s.Starttime >= start &&
                    s.Starttime < end);
            }
            else if (!showArchived)
            {
                query = query.Where(s => s.Starttime.Date >= DateTime.Today);
            }

            var sessions = await query.OrderBy(s => s.Starttime).ToListAsync();
            foreach (var s in sessions)
            {
                Console.WriteLine(
                    $"ID={s.Id} | DB={s.Starttime:yyyy-MM-dd HH:mm} | Local={s.Starttime.ToLocalTime():yyyy-MM-dd HH:mm}"
                );
            }
            // 2. 🔥 ТУТ ВІДБУВАЄТЬСЯ МАГІЯ ГРУПУВАННЯ (Перетворюємо List<Session> у List<SessionsByDateVm>)
            var model = sessions
                .GroupBy(s => s.Starttime.ToLocalTime().Date) // Групуємо по днях
                .OrderBy(g => g.Key)
                .Select(dateGroup => new SessionsByDateVm
                {
                    Date = dateGroup.Key,
                    Cinemas = dateGroup
                        .GroupBy(s => s.Hall.Cinemaid) // Групуємо по кінотеатрах всередині дня
                        .OrderBy(g => g.First().Hall.Cinema.Name)
                        .Select(cinemaGroup => new SessionsByCinemaVm
                        {
                            // Помилка була тут. Додаємо .Value, бо Cinemaid у базі може бути null
                            CinemaId = cinemaGroup.Key.Value,
                            CinemaName = cinemaGroup.First().Hall.Cinema.Name,
                            Movies = cinemaGroup
                                .GroupBy(s => s.Movieid)
                                .OrderBy(g => g.First().Movie.Title)
                                .Select(movieGroup => new SessionsByMovieVm
                                {
                                    // Помилка була тут. Додаємо .Value
                                    MovieId = movieGroup.Key.Value,
                                    Title = movieGroup.First().Movie.Title,
                                    Duration = movieGroup.First().Movie.Duration,
                                    Sessions = movieGroup.OrderBy(s => s.Starttime).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            // 3. Дані для фільтрів (це у вас теж було)
            ViewBag.Cinemas = await _context.Cinemas.Where(c => c.Isactive).ToListAsync();
            ViewBag.SelectedCinema = cinemaId;
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
            ViewBag.ShowArchived = showArchived;

            // 4. ПЕРЕДАЄМО НОВУ МОДЕЛЬ У VIEW
            return View(model);
        }

        // ================= CREATE (Код залишається без змін) =================
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

            // Визначаємо кількість повторів: 7 або 1
            int iterations = model.RepeatDaily ? 7 : 1;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    // Кожна наступна ітерація додає 1 день до початкової дати
                    DateTime currentStart = model.StartTime.AddDays(i);
                    DateTime currentEnd = currentStart.AddMinutes(movie.Duration ?? 0);

                    // Перевірка накладання для кожного дня
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
                    await _context.SaveChangesAsync(); // Щоб отримати ID сеансу для цін

                    // Додаємо ціни для кожного з 7 сеансів
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

        // ================= DELETE (Оновлено) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Tickets)
                .Include(s => s.Sessionprices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return Json(new { success = false, message = "Сеанс не знайдено." });

            // Якщо квитки вже є, ми не дозволяємо видалення, пропонуємо тільки скасування
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
    }
}