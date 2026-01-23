using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models;

public class MoviesController : Controller
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var movies = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .ToListAsync();

        return View(movies);
    }

    public async Task<IActionResult> Details(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == id);

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
    public async Task<IActionResult> Create(CreateMovieViewModel model)
    {
        if (model.Duration.HasValue && model.Duration <= 0)
        {
            ModelState.AddModelError(nameof(model.Duration),
                "Тривалість має бути більшою за 0.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Genres = _context.Genres.ToList();
            return View(model);
        }

        var movie = new Movie
        {
            Title = model.Title,
            Description = model.Description,
            Duration = model.Duration,
            Rating = model.Rating,
            Isactive = true
        };

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        foreach (var genreId in model.SelectedGenres)
        {
            _context.Moviegenres.Add(new Moviegenre
            {
                Movieid = movie.Id,
                Genreid = genreId
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}