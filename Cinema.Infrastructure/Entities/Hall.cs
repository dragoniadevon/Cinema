using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Hall
{
    public int Id { get; set; }

    public int? Cinemaid { get; set; }

    public string Name { get; set; } = null!;

    public short? Rows { get; set; }

    public short? Seatsperrow { get; set; }

    public short? Halltype { get; set; }

    public virtual Cinema? Cinema { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
