using System.Collections.Generic;

namespace Cinema.Web.Models.Sessions
{
    public class SessionsByCinemaVm
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; }

        public List<SessionsByMovieVm> Movies { get; set; }
    }
}
