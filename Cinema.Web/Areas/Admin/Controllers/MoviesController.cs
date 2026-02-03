using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MoviesController : Controller
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    // ================== HELPERS ==================

    private void FillMovieViewBags()
    {
        ViewBag.Genres = _context.Genres
            .Select(g => new { g.Id, g.Name })
            .ToList();

        ViewBag.Actors = _context.Actors
            .Select(a => new { a.Id, a.Fullname })
            .ToList();
    }

    private void ValidateMovie(CreateMovieViewModel model)
    {
        if (model.Duration.HasValue && model.Duration <= 0)
        {
            ModelState.AddModelError(
                nameof(model.Duration),
                "Тривалість має бути більшою за 0."
            );
        }

        if (model.Rating.HasValue && (model.Rating < 0 || model.Rating > 10))
        {
            ModelState.AddModelError(
                nameof(model.Rating),
                "Рейтинг має бути в межах від 0 до 10."
            );
        }
    }

    // ================== INDEX ==================

    public async Task<IActionResult> Index()
    {
        var movies = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieActors)
                .ThenInclude(ma => ma.Actor)
            .ToListAsync();

        return View(movies);
    }

    // ================== DETAILS ==================

    public async Task<IActionResult> Details(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieActors)
                .ThenInclude(ma => ma.Actor)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        return View(movie);
    }

    // ================== CREATE (GET) ==================

    public IActionResult Create()
    {
        FillMovieViewBags();
        return View(new CreateMovieViewModel());
    }

    // ================== CREATE (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMovieViewModel model)
    {
        ValidateMovie(model);

        if (!ModelState.IsValid)
        {
            FillMovieViewBags();
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
            Countrycode = model.CountryCode
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

        foreach (var actorId in model.SelectedActors)
        {
            _context.Movieactors.Add(new Movieactor
            {
                Movieid = movie.Id,
                Actorid = actorId
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ================== EDIT (GET) ==================

    public async Task<IActionResult> Edit(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .Include(m => m.MovieActors)
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
            SelectedGenres = movie.MovieGenres.Select(mg => mg.Genreid).ToList(),
            SelectedActors = movie.MovieActors.Select(ma => ma.Actorid).ToList()
        };

        FillMovieViewBags();
        return View(model);
    }

    // ================== EDIT (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateMovieViewModel model)
    {
        ValidateMovie(model);

        if (!ModelState.IsValid)
        {
            FillMovieViewBags();
            return View(model);
        }

        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .Include(m => m.MovieActors)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

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

        _context.Moviegenres.RemoveRange(movie.MovieGenres);
        _context.Movieactors.RemoveRange(movie.MovieActors);

        foreach (var genreId in model.SelectedGenres)
        {
            movie.MovieGenres.Add(new Moviegenre
            {
                Movieid = movie.Id,
                Genreid = genreId
            });
        }

        foreach (var actorId in model.SelectedActors)
        {
            movie.MovieActors.Add(new Movieactor
            {
                Movieid = movie.Id,
                Actorid = actorId
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .Include(m => m.MovieActors)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        _context.Moviegenres.RemoveRange(movie.MovieGenres);
        _context.Movieactors.RemoveRange(movie.MovieActors);
        _context.Movies.Remove(movie);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
