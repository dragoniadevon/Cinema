using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Usergenrepreference
{
    public int Userid { get; set; }

    public int Genreid { get; set; }

    public short? Score { get; set; }

    public virtual Genre Genre { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
