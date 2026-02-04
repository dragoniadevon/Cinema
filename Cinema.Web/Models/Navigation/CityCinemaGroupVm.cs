using Cinema.Infrastructure.Entities;

namespace Cinema.Web.Models.Navigation;

public class CityCinemaGroupVm
{
    public string City { get; set; } = "";

    public List<Cinema.Infrastructure.Entities.Cinema> Cinemas { get; set; } = new();
}
