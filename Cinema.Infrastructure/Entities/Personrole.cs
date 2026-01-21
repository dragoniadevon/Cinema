using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Personrole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Movieperson> Moviepeople { get; set; } = new List<Movieperson>();
}
