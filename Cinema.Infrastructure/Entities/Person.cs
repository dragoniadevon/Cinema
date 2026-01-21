using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Person
{
    public int Id { get; set; }

    public string Fullname { get; set; } = null!;

    public virtual ICollection<Movieperson> Moviepeople { get; set; } = new List<Movieperson>();
}
