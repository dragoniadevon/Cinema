using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models.Sessions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Cinema.Infrastructure.Entities.Enums;

namespace Cinema.Web.Controllers
{
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
                .Select(c => new {
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

        // ================= INDEX (ОНОВЛЕНО: ДОДАНО ФІЛЬТР ПО МІСТУ) =================
        public async Task<IActionResult> Index(int? cinemaId, string city, DateTime? date, string mode = "active")
        {
            mode = string.IsNullOrEmpty(mode) ? "active" : mode;
            ViewBag.CurrentMode = mode;

            var query = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(s => s.Tickets)
                .AsQueryable();

            // 1. Фільтрація за режимом (active/past/cancelled)
            if (mode == "past")
                query = query.Where(s => s.Isactive == true && s.Endtime < DateTime.Now);
            else if (mode == "cancelled")
                query = query.Where(s => s.Isactive == false || s.Hall.Isactive == false);
            else
                query = query.Where(s => s.Isactive == true && s.Hall.Isactive == true && s.Endtime >= DateTime.Now);

            // 2. Фільтр по місту (Додано)
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(s => s.Hall.Cinema.City == city);
            }

            // 3. Фільтр по конкретному кінотеатру
            if (cinemaId.HasValue)
            {
                query = query.Where(s => s.Hall.Cinemaid == cinemaId.Value);
            }

            // 4. Фільтр по даті
            if (date.HasValue) query = query.Where(s => s.Starttime.Date == date.Value.Date);

            var sessions = await query.OrderByDescending(s => s.Starttime).ToListAsync();

            // 5. Групування (залишається без змін)
            // 4. Групування (з захистом від Null)
            var model = sessions
                .GroupBy(s => s.Starttime.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select(dateGroup => new SessionsByDateVm
                {
                    Date = dateGroup.Key,
                    // Перевіряємо, чи є взагалі зали та кінотеатри у сеансів за цю дату
                    Cinemas = dateGroup
                        .Where(s => s.Hall?.Cinema != null) // Додаємо цей фільтр-захист
                        .GroupBy(s => s.Hall.Cinemaid)
                        .OrderBy(g => g.First().Hall.Cinema.Name)
                        .Select(cinemaGroup => new SessionsByCinemaVm
                        {
                            CinemaId = cinemaGroup.Key ?? 0,
                            CinemaName = cinemaGroup.First().Hall.Cinema.Name,
                            Movies = cinemaGroup
                                .Where(s => s.Movie != null) // Захист для фільмів
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
                .Where(d => d.Cinemas.Any()) // Не показуємо дату, якщо в ній немає валідних кінотеатрів
                .ToList();

            // 6. Дані для фільтрів у View
            // Отримуємо тільки ті міста, в яких є кінотеатри в базі
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

            await FillEditViewBags(session);
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session, int[] PriceCategoryIds, decimal[] CategoryPrices)
        {
            if (id != session.Id) return NotFound();

            // 1. Отримуємо ОРИГІНАЛЬНИЙ об'єкт із бази, щоб оновити тільки потрібні поля
            var sessionToUpdate = await _context.Sessions
                .Include(s => s.Sessionprices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sessionToUpdate == null) return NotFound();

            // 2. Отримуємо тривалість фільму для перерахунку Endtime
            var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == session.Movieid);
            if (movie == null)
            {
                ModelState.AddModelError("", "Фільм не знайдено");
                await FillEditViewBags(session);
                return View(session);
            }

            // 3. Перевірка на накладання (виключаємо поточний сеанс)
            DateTime endTime = session.Starttime.AddMinutes(movie.Duration ?? 0);
            bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                s.Id != id &&
                s.Hallid == session.Hallid &&
                session.Starttime < s.Endtime &&
                endTime > s.Starttime
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
                    // 4. Оновлюємо основні поля вручну (це найнадійніший спосіб)
                    sessionToUpdate.Movieid = session.Movieid;
                    sessionToUpdate.Hallid = session.Hallid;
                    sessionToUpdate.Starttime = session.Starttime;
                    sessionToUpdate.Endtime = endTime;
                    sessionToUpdate.Format = session.Format;
                    sessionToUpdate.Isactive = true;

                    // 5. Оновлюємо ціни
                    _context.Sessionprices.RemoveRange(sessionToUpdate.Sessionprices);

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
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Помилка при збереженні: " + ex.Message);
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
    }
}