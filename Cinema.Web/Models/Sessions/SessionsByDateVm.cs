using System;
using System.Collections.Generic;

namespace Cinema.Web.Models.Sessions
{
    public class SessionsByDateVm
    {
        public DateTime Date { get; set; }

        public bool IsAdminView { get; set; }

        public List<SessionsByCinemaVm> Cinemas { get; set; }
        public string? SelectedCinemaName { get; set; }
        public string? SelectedCinemaCity { get; set; }
        public string? SelectedCinemaAddress { get; set; }
    }
}
