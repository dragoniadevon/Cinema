using System.ComponentModel.DataAnnotations;
using Cinema.Infrastructure.Entities.Enums;

namespace Cinema.Web.Models.Sessions;

public class CreateSessionViewModel
{
    [Required(ErrorMessage = "Оберіть фільм")]
    public int MovieId { get; set; }

    [Required(ErrorMessage = "Оберіть кінотеатр")]
    public int CinemaId { get; set; }

    [Required(ErrorMessage = "Оберіть зал")]
    public int HallId { get; set; }

    [Required(ErrorMessage = "Вкажіть час початку")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Оберіть формат")]
    public SessionFormat Format { get; set; }

    public List<SessionPriceInput> Prices { get; set; } = new();
}
