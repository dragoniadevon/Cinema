using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

public class GenresController : Controller
{
    private readonly AppDbContext _context;

    public GenresController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Genres
    public async Task<IActionResult> Index()
    {
        var genres = await _context.Genres.ToListAsync();
        return View(genres);
    }

    // GET: Genres/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Genres/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Genre genre)
    {
        // 1. Нормалізація
        if (genre.Name != null)
        {
            genre.Name = genre.Name.Trim();
        }

        if (string.IsNullOrWhiteSpace(genre.Name))
        {
            ModelState.AddModelError(nameof(genre.Name), "Назва жанру обовʼязкова");
            return View(genre);
        }

        try
        {
            // 2. Єдине джерело істини — БД
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 3. БД сказала: unique violation
            ModelState.AddModelError(nameof(genre.Name), "Такий жанр вже існує");
            return View(genre);
        }

        // 4. PRG
        return RedirectToAction(nameof(Index));
    }

    // GET: Genres/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null)
            return NotFound();

        return View(genre);
    }

    // POST: Genres/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Genre genre)
    {
        if (id != genre.Id)
            return BadRequest();

        if (genre.Name != null)
        {
            genre.Name = genre.Name.Trim();
        }

        if (string.IsNullOrWhiteSpace(genre.Name))
        {
            ModelState.AddModelError(nameof(genre.Name), "Назва жанру обовʼязкова");
            return View(genre);
        }

        try
        {
            _context.Update(genre);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(genre.Name), "Такий жанр вже існує");
            return View(genre);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null)
            return NotFound();

        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

}