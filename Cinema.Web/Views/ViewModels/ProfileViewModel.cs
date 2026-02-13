using System.Collections.Generic;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<ActiveTicketVm> ActiveTickets { get; set; } = new();
        public List<HistoryTicketVm> HistoryTickets { get; set; } = new();
    }
}
