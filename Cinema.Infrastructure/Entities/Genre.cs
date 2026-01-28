using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Moviegenre> MovieGenres { get; set; }
    = new List<Moviegenre>();
}
