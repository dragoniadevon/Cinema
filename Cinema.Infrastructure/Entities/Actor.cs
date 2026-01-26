namespace Cinema.Infrastructure.Entities;

public class Actor
{
    public int Id { get; set; }
    public string Fullname { get; set; } = null!;

    public ICollection<Movieactor> Movieactors { get; set; } = new List<Movieactor>();
}
