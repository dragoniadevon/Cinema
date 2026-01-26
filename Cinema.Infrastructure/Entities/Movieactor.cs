namespace Cinema.Infrastructure.Entities;

public class Movieactor
{
    public int Movieid { get; set; }
    public Movie Movie { get; set; } = null!;

    public int Actorid { get; set; }
    public Actor Actor { get; set; } = null!;
}
