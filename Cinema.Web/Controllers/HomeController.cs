using Cinema.Infrastructure.Entities;
using Cinema.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Cinema.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Sessions", new { area = "Admin" });
        }

        var cinemaIdCookie = Request.Cookies["selectedCinemaId"];

        if (int.TryParse(cinemaIdCookie, out int id))
        {
            ViewBag.SelectedCinemaId = id;
        }

        return View();
    }

    public IActionResult PosterCarouselPartial()
    {
        var cinemaIdCookie = Request.Cookies["SelectedCinemaId"];

        int? cinemaId = null;
        if (int.TryParse(cinemaIdCookie, out var id))
        {
            cinemaId = id;
        }

        return ViewComponent("PosterCarousel", new { cinemaId });
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> GetSessionsForDate(int movieId, int cinemaId, string date)
    {
        if (!DateTime.TryParse(date, out DateTime selectedDate)) return BadRequest();

        var now = DateTime.Now;

        var sessions = await _db.Sessions
            .Include(s => s.Sessionprices)
            .Where(s => s.Movieid == movieId &&
                        s.Hall.Cinemaid == cinemaId &&
                        s.Isactive == true &&
                        s.Starttime.Date == selectedDate.Date &&
                        s.Starttime > now)
            .OrderBy(s => s.Starttime)
            .Select(s => new {
                sessionId = s.Id,
                startTime = s.Starttime.ToString("HH:mm"),
                minPrice = s.Sessionprices.Any() ? s.Sessionprices.Min(p => p.Price).ToString("0") : "0"
            })
            .ToListAsync();

        return Json(sessions);
    }
}