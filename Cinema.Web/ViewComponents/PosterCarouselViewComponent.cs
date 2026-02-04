using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.ViewComponents;

public class PosterCarouselViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public PosterCarouselViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? cinemaId)
    {
        // 1. Отримуємо поточний час (дата + година + хвилина)
        var now = DateTime.Now;

        // 2. Починаємо формувати запит
        var query = _db.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            // 🔥 Фільтруємо: тільки активні та тільки ТІ, ЩО ЩЕ НЕ РОЗПОЧАЛИСЯ
            .Where(s => s.Isactive == true && s.Starttime > now);

        // 3. Фільтруємо за кінотеатром, якщо він обраний
        if (cinemaId.HasValue)
        {
            query = query.Where(s => s.Hall!.Cinemaid == cinemaId.Value);
        }

        // 4. Групуємо та вибираємо дані для афіш
        var movies = await query
            .Select(s => s.Movie)
            .Distinct()
            .Select(m => new PosterCarouselItemVm
            {
                MovieId = m!.Id,
                Title = m.Title,
                PosterUrl = m.Posterurl,
                TrailerUrl = m.Trailerurl
            })
            .ToListAsync();

        return View(movies);
    }

}
