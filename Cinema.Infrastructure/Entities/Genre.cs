using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Usergenrepreference> Usergenrepreferences { get; set; } = new List<Usergenrepreference>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
