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

        // GET: Sessions
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .OrderBy(s => s.Starttime)
                .ToListAsync();

            return View(sessions);
        }

        // GET: Sessions/Create
        public IActionResult Create()
        {
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Halls = _context.Halls.ToList();
            return View();
        }

        // POST: Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
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
                ViewBag.Movies = _context.Movies.ToList();
                ViewBag.Halls = _context.Halls.ToList();
                return View(session);
            }

            _context.Sessions.Add(session);

            // АКТИВУЄМО ФІЛЬМ
            if (!movie.Isactive)
            {
                movie.Isactive = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
