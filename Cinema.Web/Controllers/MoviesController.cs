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
    public async Task<IActionResult> Details(int id, string city, string date)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null) return NotFound();

        string currentCity = city ?? Request.Cookies["selectedCity"];
        var now = DateTime.Now;

        var availableDatesQuery = _context.Sessions
            .Where(s => s.Movieid == id && s.Isactive == true && s.Starttime > now);

        if (!string.IsNullOrEmpty(currentCity))
        {
            availableDatesQuery = availableDatesQuery.Where(s => s.Hall.Cinema.City == currentCity);
        }

        var availableDates = await availableDatesQuery
            .Select(s => s.Starttime.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        DateTime filterDate;
        if (!DateTime.TryParse(date, out filterDate) || !availableDates.Contains(filterDate.Date))
        {
            filterDate = availableDates.FirstOrDefault();
            if (filterDate == default) filterDate = DateTime.Today;
        }

        var sessionsQuery = _context.Sessions
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .Include(s => s.Sessionprices)
            .Where(s => s.Movieid == id &&
                s.Isactive == true &&
                s.Starttime.Date == filterDate.Date &&
                s.Starttime > now.AddMinutes(-15));

        if (!string.IsNullOrEmpty(currentCity))
        {
            sessionsQuery = sessionsQuery.Where(s => s.Hall.Cinema.City == currentCity);
        }

        var sessions = await sessionsQuery
            .OrderBy(s => s.Hall.Cinema.Name)
            .ThenBy(s => s.Starttime)
            .ToListAsync();

        ViewBag.Sessions = sessions;
        ViewBag.SelectedCity = currentCity;
        ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");
        ViewBag.AvailableDates = availableDates;

        return View(movie);
    }
}