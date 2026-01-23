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
        ViewBag.Genres = _context.Genres
            .Select(g => new
            {
                g.Id,
                g.Name
            })
            .ToList();

        return View(new CreateMovieViewModel());
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
            Releasedate = model.ReleaseDate,
            Rating = model.Rating,
            Posterurl = model.PosterUrl,
            Trailerurl = model.TrailerUrl,
            Agerating = model.AgeRating,
            Languagecode = model.LanguageCode,
            Countrycode = model.CountryCode,
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

    public async Task<IActionResult> Edit(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        var model = new CreateMovieViewModel
        {
            Title = movie.Title,
            Description = movie.Description,
            Duration = movie.Duration,
            ReleaseDate = movie.Releasedate,
            Rating = movie.Rating,
            PosterUrl = movie.Posterurl,
            TrailerUrl = movie.Trailerurl,
            AgeRating = movie.Agerating,
            LanguageCode = movie.Languagecode,
            CountryCode = movie.Countrycode,
            SelectedGenres = movie.MovieGenres
                .Select(mg => mg.Genreid)
                .ToList()
        };

        ViewBag.Genres = _context.Genres
            .Select(g => new { g.Id, g.Name })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateMovieViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Genres = _context.Genres.ToList();
            return View(model);
        }

        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        // 🔁 оновлюємо поля
        movie.Title = model.Title;
        movie.Description = model.Description;
        movie.Duration = model.Duration;
        movie.Releasedate = model.ReleaseDate;
        movie.Rating = model.Rating;
        movie.Posterurl = model.PosterUrl;
        movie.Trailerurl = model.TrailerUrl;
        movie.Agerating = model.AgeRating;
        movie.Languagecode = model.LanguageCode;
        movie.Countrycode = model.CountryCode;

        // 🧹 видаляємо старі жанри
        _context.Moviegenres.RemoveRange(movie.MovieGenres);

        // ➕ додаємо нові
        foreach (var genreId in model.SelectedGenres)
        {
            movie.MovieGenres.Add(new Moviegenre
            {
                Movieid = movie.Id,
                Genreid = genreId
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}