using Microsoft.AspNetCore.Mvc;
using Cinema.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Web.Controllers
{
    public class CinemasController : Controller
    {
        private readonly AppDbContext _context;

        public CinemasController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var cinemas = await _context.Cinemas
                .Include(c => c.Halls)
                .ToListAsync();
            return View(cinemas);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cinema.Infrastructure.Entities.Cinema cinema)
        {
            if (ModelState.IsValid)
            {
                _context.Cinemas.Add(cinema);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cinema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Halls).ThenInclude(h => h.Sessions).ThenInclude(s => s.Tickets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cinema == null) return NotFound();

            bool hasTickets = cinema.Halls.SelectMany(h => h.Sessions).SelectMany(s => s.Tickets).Any();

            if (hasTickets)
            {
                TempData["Error"] = "Неможливо видалити кінотеатр: на сеанси в його залах вже продано квитки!";
                return RedirectToAction(nameof(Index));
            }

            _context.Cinemas.Remove(cinema);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Кінотеатр успішно видалено!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null) return NotFound();
            return View(cinema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cinema.Infrastructure.Entities.Cinema cinema)
        {
            if (id != cinema.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(cinema);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cinema);
        }
    }
}