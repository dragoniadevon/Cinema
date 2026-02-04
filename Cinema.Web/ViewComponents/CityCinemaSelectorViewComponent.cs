using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models.Navigation;

namespace Cinema.Web.ViewComponents;

public class CityCinemaSelectorViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public CityCinemaSelectorViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cinemas = await _db.Cinemas
            .Where(c => c.Isactive)
            .OrderBy(c => c.City)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var model = cinemas
            .GroupBy(c => c.City)
            .Select(g => new CityCinemaGroupVm
            {
                City = g.Key ?? "Невідоме місто",
                Cinemas = g.ToList()
            })
            .ToList();

        return View(model);
    }
}
