using System.Collections.Generic;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<Ticket> Tickets { get; set; } = new();
    }
}
