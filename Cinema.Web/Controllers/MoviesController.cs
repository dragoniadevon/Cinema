using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Cinema.Web.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cinema.Web.Controllers;

public class MoviesController : Controller
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    // ================== ДЕТАЛІ ФІЛЬМУ ДЛЯ КЛІЄНТА ==================
    // Додано підтримку параметрів 'city' та 'date' для точної фільтрації
    public async Task<IActionResult> Details(int id, string city, string date, int? cinemaId)
    {
        // Отримуємо дані фільму
        var movie = await _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null) return NotFound();

        // 1. Отримуємо місто з Cookie, якщо воно не передано в URL
        string currentCity = city ?? Request.Cookies["selectedCity"];

        // 2. Визначаємо дату фільтрації
        DateTime filterDate;
        if (!DateTime.TryParse(date, out filterDate))
        {
            filterDate = DateTime.Today;
        }

        // 3. Базовий запит: отримуємо ВСІ активні сеанси фільму в обраному МІСТІ
        var sessionsQuery = _context.Sessions
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .Where(s => s.Movieid == id && s.Isactive == true);

        // Фільтр за датою
        sessionsQuery = sessionsQuery.Where(s => s.Starttime.Date == filterDate.Date);

        // Якщо місто вибрано, показуємо сеанси лише в цьому місті (у всіх кінотеатрах міста)
        if (!string.IsNullOrEmpty(currentCity))
        {
            sessionsQuery = sessionsQuery.Where(s => s.Hall.Cinema.City == currentCity);
        }

        var sessions = await sessionsQuery
            .OrderBy(s => s.Hall.Cinema.Name)
            .ThenBy(s => s.Starttime)
            .ToListAsync();

        // Передаємо дані у ViewBag
        ViewBag.Sessions = sessions;
        ViewBag.SelectedCity = currentCity;
        ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");

        return View(movie);
    }
}