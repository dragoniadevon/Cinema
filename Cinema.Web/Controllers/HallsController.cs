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

    // Форма створення залу
    public IActionResult Create(int cinemaId)
    {
        ViewBag.CinemaId = cinemaId;
        return View();
    }

    // Перегляд схеми залу
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
}