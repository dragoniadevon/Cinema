using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

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
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .OrderBy(s => s.Starttime)
                .ToListAsync();

            return View(sessions);
        }

        // ================= CREATE =================
        public IActionResult Create()
{
    ViewBag.Movies = _context.Movies.ToList();
    ViewBag.Cinemas = _context.Cinemas.ToList();
    ViewBag.Halls = new List<Hall>(); // поки пусто
    return View();
}

[HttpGet]
public IActionResult GetHallsByCinema(int cinemaId)
{
    var halls = _context.Halls
        .Where(h => h.Cinemaid == cinemaId)
        .Select(h => new
        {
            h.Id,
            h.Name,
            h.Halltype,      // тип залу (звичайний, 3D)
            h.Rows,
            h.Seatsperrow    // щоб знати чи є нумеровані місця
        })
        .ToList();

    return Json(halls);
}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Halls = _context.Halls.ToList();

            var movie = await _context.Movies.FindAsync(session.Movieid);
            if (movie == null)
            {
                ModelState.AddModelError("", "Фільм не знайдено");
                return View(session);
            }

            session.Endtime = session.Starttime.AddMinutes(movie.Duration ?? 0);

            bool isOverlapping = await _context.Sessions.AnyAsync(s =>
                s.Hallid == session.Hallid &&
                session.Starttime < s.Endtime &&
                session.Endtime > s.Starttime
            );

            if (isOverlapping)
            {
                ModelState.AddModelError("", "У цьому залі вже є сеанс у вибраний час");
                return View(session);
            }

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

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

            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Halls = _context.Halls.ToList();

            var movie = await _context.Movies.FindAsync(session.Movieid);
            if (movie == null)
            {
                ModelState.AddModelError("", "Фільм не знайдено");
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
