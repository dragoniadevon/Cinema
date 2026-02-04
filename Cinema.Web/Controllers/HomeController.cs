using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cinema.Web.Models;

namespace Cinema.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
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
}