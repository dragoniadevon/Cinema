using Microsoft.AspNetCore.Mvc;
using Cinema.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

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
            .OrderByDescending(c => c.Isactive)
            .ThenBy(c => c.Name)
            .Select(c => new Cinema.Infrastructure.Entities.Cinema
            {
                Id = c.Id,
                Name = c.Name,
                City = c.City,
                Address = c.Address,
                Isactive = c.Isactive,
                Halls = c.Halls.OrderBy(h => h.Name).ToList()
            })
            .ToListAsync();

        return View(cinemas);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cinema.Infrastructure.Entities.Cinema cinema)
    {
        bool nameInCityExists = await _context.Cinemas.AnyAsync(c =>
            c.Name.ToLower() == cinema.Name.ToLower() &&
            c.City.ToLower() == cinema.City.ToLower());

        bool addressInCityExists = await _context.Cinemas.AnyAsync(c =>
            c.City.ToLower() == cinema.City.ToLower() &&
            c.Address.ToLower() == cinema.Address.ToLower());

        if (nameInCityExists)
        {
            ModelState.AddModelError("Name", "Кінотеатр з такою назвою вже зареєстрований у цьому місті.");
        }

        if (addressInCityExists)
        {
            ModelState.AddModelError("Address", "В цьому місті, за цією адресою вже зареєстровано кінотеатр.");
        }

        if (ModelState.IsValid)
        {
            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cinema);
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

        bool nameDuplicate = await _context.Cinemas.AnyAsync(c =>
            c.Id != id &&
            c.Name.ToLower() == cinema.Name.ToLower() &&
            c.City.ToLower() == cinema.City.ToLower());

        bool addressDuplicate = await _context.Cinemas.AnyAsync(c =>
            c.Id != id &&
            c.City.ToLower() == cinema.City.ToLower() &&
            c.Address.ToLower() == cinema.Address.ToLower());

        if (nameDuplicate)
        {
            ModelState.AddModelError("Name", "Інший кінотеатр у цьому місті вже використовує цю назву.");
        }

        if (addressDuplicate)
        {
            ModelState.AddModelError("Address", "Ця адреса в цьому місті вже закріплена за іншим об'єктом у базі.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cinema);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Cinemas.Any(e => e.Id == cinema.Id)) return NotFound();
                else throw;
            }
        }
        return View(cinema);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var cinema = await _context.Cinemas.Include(c => c.Halls).FirstOrDefaultAsync(m => m.Id == id);
        if (cinema == null) return NotFound();

        cinema.Isactive = !cinema.Isactive;
        foreach (var hall in cinema.Halls) { hall.Isactive = cinema.Isactive; }

        _context.Update(cinema);
        await _context.SaveChangesAsync();
        TempData["Success"] = cinema.Isactive ? "Кінотеатр та його зали відновлено!" : "Об'єкт перенесено в архів.";
        return RedirectToAction(nameof(Index));
    }
}