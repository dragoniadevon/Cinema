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

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Usergenrepreference> Usergenrepreferences { get; set; } = new List<Usergenrepreference>();
}
