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

            ViewBag.Halls = model.CinemaId > 0
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
            if (model == null) return BadRequest();

            if (model.StartTime < DateTime.Now)
                ModelState.AddModelError(nameof(model.StartTime), "Час початку не може бути в минулому.");

            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null || movie.Duration <= 0)
                ModelState.AddModelError(nameof(model.MovieId), "Оберіть дійсний фільм з вказаною тривалістю.");

            var hall = await _context.Halls.FindAsync(model.HallId);
            if (hall == null || !hall.Isactive)
                ModelState.AddModelError(nameof(model.HallId), "Обраний зал недоступний або не існує.");

            if (!ModelState.IsValid)
            {
                FillCreateViewBags(model);
                return View(model);
            }

            var allCategories = await _context.Pricecategories.ToListAsync();
            var hallType = hall.Halltype ?? 0;

            var allowedNames = hallType switch
            {
                1 => new[] { "Standard" },
                2 => new[] { "VIP" },
                3 => new[] { "Standard", "VIP" },
                _ => Array.Empty<string>()
            };

            var allowedCategoryIds = allCategories
                .Where(pc => allowedNames.Any(name => name.Equals(pc.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(pc => pc.Id)
                .ToHashSet();

            for (int i = 0; i < model.Prices.Count; i++)
            {
                var p = model.Prices[i];
                if (allowedCategoryIds.Contains(p.PriceCategoryId))
                {
                    if (p.Price <= 0)
                    {
                        var catName = allCategories.FirstOrDefault(c => c.Id == p.PriceCategoryId)?.Name;
                        ModelState.AddModelError($"Prices[{i}].Price", $"Вкажіть ціну для категорії «{catName}».");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                FillCreateViewBags(model);
                return View(model);
            }

            var sessionStart = model.StartTime;
            var sessionEnd = model.StartTime.AddMinutes(movie.Duration ?? 0);

            bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                s.Hallid == hall.Id && sessionStart < s.Endtime && sessionEnd > s.Starttime);

            if (isOverlapping)
            {
                ModelState.AddModelError("", "У цьому залі вже є сеанс у вибраний час.");
                FillCreateViewBags(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
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

                var pricesToAdd = model.Prices
                    .Where(p => p.Price > 0 && allowedCategoryIds.Contains(p.PriceCategoryId))
                    .Select(p => new Sessionprice
                    {
                        Sessionid = session.Id,
                        Categoryid = p.PriceCategoryId,
                        Price = p.Price
                    }).ToList();

                if (pricesToAdd.Any()) _context.Sessionprices.AddRange(pricesToAdd);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Помилка бази даних. Спробуйте ще раз.");
                FillCreateViewBags(model);
                return View(model);
            }
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Halls = await _context.Halls
                .Where(h => h.Cinemaid == session.Hall.Cinemaid && h.Isactive)
                .ToListAsync();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            if (id != session.Id) return NotFound();

            var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == session.Movieid);
            if (movie == null)
            {
                ModelState.AddModelError("", "Фільм не знайдено");
                ViewBag.Movies = await _context.Movies.ToListAsync();
                ViewBag.Halls = await _context.Halls.ToListAsync();
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
                ViewBag.Movies = await _context.Movies.ToListAsync();
                ViewBag.Halls = await _context.Halls.ToListAsync();
                return View(session);
            }

            if (ModelState.IsValid)
            {
                _context.Update(session);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(session);
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
