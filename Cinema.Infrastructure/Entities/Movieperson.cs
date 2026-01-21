using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Movieperson
{
    public int Movieid { get; set; }

    public int Personid { get; set; }

    public int Roleid { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Person Person { get; set; } = null!;

    public virtual Personrole Role { get; set; } = null!;
}
