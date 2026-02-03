using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using System.Threading.Tasks;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class GenresController : Controller
{
    private readonly AppDbContext _context;


    public GenresController(AppDbContext context)
    {
        _context = context;
    }

    // ================== HELPERS ==================

    private bool ValidateGenre(Genre genre)
    {
        if (genre.Name != null)
        {
            genre.Name = genre.Name.Trim();
        }

        if (string.IsNullOrWhiteSpace(genre.Name))
        {
            ModelState.AddModelError(
                nameof(genre.Name),
                "Назва жанру обовʼязкова"
            );
            return false;
        }

        return true;
    }

    // ================== INDEX ==================

    public async Task<IActionResult> Index()
    {
        var genres = await _context.Genres.ToListAsync();
        return View(genres);
    }

    // ================== CREATE (GET) ==================

    public IActionResult Create()
    {
        return View();
    }

    // ================== CREATE (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Genre genre)
    {
        if (!ValidateGenre(genre))
        {
            return View(genre);
        }

        try
        {
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(genre.Name),
                "Такий жанр вже існує"
            );
            return View(genre);
        }

        return RedirectToAction(nameof(Index));
    }

    // ================== EDIT (GET) ==================

    public async Task<IActionResult> Edit(int id)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null)
            return NotFound();

        return View(genre);
    }

    // ================== EDIT (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Genre genre)
    {
        if (id != genre.Id)
            return BadRequest();

        if (!ValidateGenre(genre))
        {
            return View(genre);
        }

        try
        {
            _context.Update(genre);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(genre.Name),
                "Такий жанр вже існує"
            );
            return View(genre);
        }

        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================

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
