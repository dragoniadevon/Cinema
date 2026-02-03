using System;
using System.Collections.Generic;

namespace Cinema.Web.Models.Sessions
{
    public class SessionsByDateVm
    {
        public DateTime Date { get; set; }

        public bool IsAdminView { get; set; }

        public List<SessionsByCinemaVm> Cinemas { get; set; }
    }
}
