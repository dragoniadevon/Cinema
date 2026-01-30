using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models.Sessions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Cinema.Web.Controllers
{
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        // Заповнює ViewBag для Create (і для повернення форми при помилках)
        private void FillCreateViewBags(CreateSessionViewModel model)
        {
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();

            // Якщо є CinemaId — підтягнути зали; інакше порожній список
            ViewBag.Halls = model != null && model.CinemaId > 0
                ? _context.Halls.Where(h => h.Cinemaid == model.CinemaId && h.Isactive).ToList()
                : new List<Hall>();
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .OrderBy(s => s.Starttime)
                .ToListAsync();

            return View(sessions);
        }

        // ================= CREATE (GET) =================
        public IActionResult Create()
        {
            var now = DateTime.Now;
            var model = new CreateSessionViewModel
            {
                // Обрізаємо секунди
                StartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),

                // Ініціалізація цін — беремо всі категорії з БД
                Prices = _context.Pricecategories
                    .Select(pc => new SessionPriceInput
                    {
                        PriceCategoryId = pc.Id,
                        CategoryName = pc.Name,
                        Price = 0m
                    })
                    .ToList()
            };

            // Заповнюємо ViewBag (фільми/кінотеатри)
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Cinemas = _context.Cinemas.Where(c => c.Isactive).ToList();
            ViewBag.Halls = new List<Hall>();

            return View(model);
        }

        // API для підвантаження залів при виборі кінотеатру
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

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSessionViewModel model)
        {
            // 0️⃣ ЄДИНА перевірка на null
            if (model == null)
            {
                return BadRequest();
            }

            // 1️⃣ Базова валідація
            if (model.StartTime < DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.StartTime),
                    "Час початку не може бути в минулому."
                );
            }

            if (model.MovieId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.MovieId),
                    "Оберіть фільм."
                );
            }

            if (model.HallId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.HallId),
                    "Оберіть зал."
                );
            }

            if (!ModelState.IsValid)
            {
                FillCreateViewBags(model);
                return View(model);
            }

            // 2️⃣ Завантаження фільму
            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null)
            {
                ModelState.AddModelError(
                    nameof(model.MovieId),
                    "Фільм не знайдено."
                );

                FillCreateViewBags(model);
                return View(model);
            }

            // ⬇️ далі код без змін


            // Перевірка, чи в фільма задана тривалість
            if (movie.Duration == null || movie.Duration <= 0)
            {
                ModelState.AddModelError(nameof(model.MovieId), "Не вказана коректна тривалість фільму.");
                FillCreateViewBags(model);
                return View(model);
            }

            var hall = await _context.Halls.FindAsync(model.HallId);
            if (hall == null || !hall.Isactive)
            {
                ModelState.AddModelError(nameof(model.HallId), "Обраний зал недоступний.");
                FillCreateViewBags(model);
                return View(model);
            }

            // Завантажуємо всі категорії цін (щоб зіставляти id → name)
            var allCategories = await _context.Pricecategories.ToListAsync();
            var categoriesById = allCategories.ToDictionary(c => c.Id, c => c.Name);

            // Визначаємо набори категорій, що дозволені для конкретного залу
            var hallType = hall.Halltype ?? 0;
            string[] allowedCategoryNames = hallType switch
            {
                1 => new[] { "Standard" },                 // Standard
                2 => new[] { "VIP" },                      // VIP
                3 => new[] { "Standard", "VIP" },          // Mixed
                _ => Array.Empty<string>()
            };

            // Отримуємо Id потрібних категорій (ті, що є у БД)
            var allowedCategoryIds = allCategories
                .Where(pc => allowedCategoryNames.Contains(pc.Name))
                .Select(pc => pc.Id)
                .ToHashSet();

            // --- 3. Валідуємо ціни, уже маючи мапу categoryId→name ---
            // Перевіримо, що для кожної обов'язкової категорії встановлена ціна > 0
            for (int i = 0; i < model.Prices.Count; i++)
            {
                var p = model.Prices[i];

                // нас цікавлять ТІЛЬКИ дозволені категорії
                if (!allowedCategoryIds.Contains(p.PriceCategoryId))
                    continue;

                if (p.Price <= 0)
                {
                    ModelState.AddModelError(
                        $"Prices[{i}].Price",
                        $"Для категорії «{categoriesById[p.PriceCategoryId]}» потрібно вказати ціну більше 0."
                    );
                }
            }


            // Якщо є помилки — повернути форму
            if (!ModelState.IsValid)
            {
                FillCreateViewBags(model);
                return View(model);
            }

            // --- 4. Готуємо об'єкт сеансу та перевіряємо накладення ---
            var sessionStart = model.StartTime;
            var sessionEnd = model.StartTime.AddMinutes(movie.Duration!.Value);

            bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                s.Hallid == hall.Id &&
                sessionStart < s.Endtime &&
                sessionEnd > s.Starttime
            );

            if (isOverlapping)
            {
                ModelState.AddModelError("", "У цьому залі вже є сеанс у вибраний час.");
                FillCreateViewBags(model);
                return View(model);
            }

            // --- 5. Зберігаємо сеанс і ціни в транзакції для надійності ---
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var session = new Session
                    {
                        Movieid = model.MovieId,
                        Hallid = model.HallId,
                        Starttime = sessionStart,
                        Endtime = sessionEnd,
                        Format = (short)model.Format,
                        Isactive = true
                    };

                    _context.Sessions.Add(session);
                    await _context.SaveChangesAsync();

                    // Додаємо лише ті ціни, де Price > 0 та категорія дозволена
                    var pricesToAdd = model.Prices
                        .Where(p => p.Price > 0 && allowedCategoryIds.Contains(p.PriceCategoryId))
                        .Select(p => new Sessionprice
                        {
                            Sessionid = session.Id,
                            Categoryid = p.PriceCategoryId,
                            Price = p.Price
                        })
                        .ToList();

                    if (pricesToAdd.Any())
                    {
                        _context.Sessionprices.AddRange(pricesToAdd);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (DbUpdateException)
                {
                    // Логувати ex.InnerException при потребі
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Помилка збереження у базі даних. Спробуйте пізніше.");
                    FillCreateViewBags(model);
                    return View(model);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Halls = _context.Halls.ToList();
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            if (id != session.Id) return NotFound();

            var movie = await _context.Movies.FindAsync(session.Movieid);
            if (movie == null)
            {
                ModelState.AddModelError("", "Фільм не знайдено");
                ViewBag.Movies = _context.Movies.ToList();
                ViewBag.Halls = _context.Halls.ToList();
                return View(session);
            }

            session.Endtime = session.Starttime.AddMinutes(movie.Duration ?? 0);

            bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                s.Id != session.Id &&
                s.Hallid == session.Hallid &&
                session.Starttime < s.Endtime &&
                session.Endtime > s.Starttime
            );

            if (isOverlapping)
            {
                ModelState.AddModelError("", "У цьому залі вже є сеанс у вибраний час");
                ViewBag.Movies = _context.Movies.ToList();
                ViewBag.Halls = _context.Halls.ToList();
                return View(session);
            }

            _context.Update(session);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
