using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class User
{
    public int Id { get; set; }

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public string Email { get; set; } = null!;

    public string? Passwordhash { get; set; }

    public int? Roleid { get; set; }

    public DateTime? Createdat { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
