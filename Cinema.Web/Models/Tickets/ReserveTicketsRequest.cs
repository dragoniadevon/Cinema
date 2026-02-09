using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cinema.Web.Models.Tickets;

public class ReserveTicketsRequest
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    public List<int> SeatIds { get; set; } = new();
}
