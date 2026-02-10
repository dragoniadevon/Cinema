using System.Collections.Generic;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;

        // Активні квитки (видно у вкладці "Активні")
        public List<Ticket> ActiveTickets { get; set; } = new();

        // Історія дій (купівлі, повернення, скасування)
        public List<Ticket> HistoryTickets { get; set; } = new();
    }
}
