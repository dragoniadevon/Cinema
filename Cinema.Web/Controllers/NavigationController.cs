using Microsoft.AspNetCore.Mvc;

namespace Cinema.Web.Controllers;

public class NavigationController : Controller
{
    [HttpPost]
    public IActionResult SelectCinema(
        string city,
        int cinemaId,
        string cinemaName)
    {
        Response.Cookies.Append("SelectedCity", city);
        Response.Cookies.Append("SelectedCinemaId", cinemaId.ToString());
        Response.Cookies.Append("SelectedCinemaName", cinemaName);

        return Ok();
    }
}
