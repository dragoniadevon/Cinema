using Cinema.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HallsController : Controller
{
    private readonly AppDbContext _context;

    public HallsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Create(int cinemaId)
    {
        ViewBag.CinemaId = cinemaId;
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var hall = await _context.Halls
            .Include(h => h.Seats)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null)
        {
            return NotFound();
        }

        return View(hall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Hall hall)
    {
        if (ModelState.IsValid)
        {
            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();

            for (short r = 1; r <= hall.Rows; r++)
            {
                var rowSeats = new List<Seat>();
                for (short s = 1; s <= hall.Seatsperrow; s++)
                {
                    rowSeats.Add(new Seat
                    {
                        Hallid = hall.Id,
                        Rownumber = r,
                        Seatnumber = s
                    });
                }
                _context.Seats.AddRange(rowSeats);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Зал '{hall.Name}' та {hall.Rows * hall.Seatsperrow} місць успішно створені!";
            return RedirectToAction("Index", "Cinemas");
        }
        return View(hall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hall = await _context.Halls
            .Include(h => h.Sessions).ThenInclude(s => s.Tickets)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (hall == null) return NotFound();

        if (hall.Sessions.SelectMany(s => s.Tickets).Any())
        {
            TempData["Error"] = "Неможливо видалити зал: на сеанси в цьому залі вже продано квитки!";
            return RedirectToAction("Index", "Cinemas");
        }

        _context.Halls.Remove(hall);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Зал та його місця видалено!";
        return RedirectToAction("Index", "Cinemas");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var hall = await _context.Halls.FindAsync(id);
        if (hall == null) return NotFound();
        return View(hall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Hall hall)
    {
        if (id != hall.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(hall);
                await _context.SaveChangesAsync();

                var oldSeats = _context.Seats.Where(s => s.Hallid == hall.Id);
                _context.Seats.RemoveRange(oldSeats);
                await _context.SaveChangesAsync();

                var newSeats = new List<Seat>();
                for (short r = 1; r <= hall.Rows; r++)
                {
                    for (short s = 1; s <= hall.Seatsperrow; s++)
                    {
                        newSeats.Add(new Seat
                        {
                            Hallid = hall.Id,
                            Rownumber = r,
                            Seatnumber = s
                        });
                    }
                }
                _context.Seats.AddRange(newSeats);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Зал '{hall.Name}' оновлено. Місця перегенеровано!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Halls.Any(e => e.Id == hall.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction("Index", "Cinemas");
        }
        return View(hall);
    }
}