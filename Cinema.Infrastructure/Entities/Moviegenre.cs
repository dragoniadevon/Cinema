namespace Cinema.Infrastructure.Entities;

public class Moviegenre
{
    public int Movieid { get; set; }
    public Movie Movie { get; set; } = null!;

    public int Genreid { get; set; }
    public Genre Genre { get; set; } = null!;
}