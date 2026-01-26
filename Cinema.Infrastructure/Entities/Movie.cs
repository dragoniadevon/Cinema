using Cinema.Infrastructure.Entities.Enums;
namespace Cinema.Infrastructure.Entities;


public class Movie
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public short? Duration { get; set; }

    public DateOnly? Releasedate { get; set; }

    public decimal? Rating { get; set; }

    public string? Posterurl { get; set; }

    public string? Trailerurl { get; set; }

    // 👇 ENUM-и
    public AgeRating? Agerating { get; set; }

    public LanguageCode? Languagecode { get; set; }

    public CountryCode? Countrycode { get; set; }

    public bool Isactive { get; set; } = true;

    // Навігація
    public ICollection<Moviegenre> MovieGenres { get; set; } = new List<Moviegenre>();
    public ICollection<Movieactor> MovieActors { get; set; } = new List<Movieactor>();
}
