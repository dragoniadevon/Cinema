using System;
using System.Collections.Generic;

namespace Cinema.Infrastructure.Entities;

public partial class Movie
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public short? Duration { get; set; }

    public DateOnly? Releasedate { get; set; }

    public decimal? Rating { get; set; }

    public string? Posterurl { get; set; }

    public string? Trailerurl { get; set; }

    public short? Agerating { get; set; }

    public string? Languagecode { get; set; }

    public string? Countrycode { get; set; }

    public bool? Isactive { get; set; }

    public virtual ICollection<Movieperson> Moviepeople { get; set; } = new List<Movieperson>();

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Moviegenre> MovieGenres { get; set; }
    = new List<Moviegenre>();
}
