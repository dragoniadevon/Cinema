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
}