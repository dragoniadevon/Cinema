using Cinema.Infrastructure.Entities;
using System.Collections.Generic;

namespace Cinema.Web.Models.Sessions
{
    public class SessionsByMovieVm
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public int? Duration { get; set; }

        public List<Session> Sessions { get; set; }
    }
}
