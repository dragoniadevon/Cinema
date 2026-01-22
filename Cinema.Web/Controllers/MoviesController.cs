using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

public class MoviesController : Controller
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Цей рядок просто намагається отримати список фільмів з бази
        var movies = await _context.Movies.ToListAsync();
        return View(movies);
    }

    public async Task<IActionResult> Details(int id)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    // GET: Movies/Create
    public IActionResult Create()
    {
        ViewBag.Genres = _context.Genres.ToList();
        return View();
    }

    // POST: Movies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie)
    {
        // Серверна валідація тривалості
        if (movie.Duration.HasValue && movie.Duration <= 0)
        {
            ModelState.AddModelError(nameof(movie.Duration), "Тривалість має бути більшою за 0.");
        }

        if (ModelState.IsValid)
        {
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(movie);
    }

}