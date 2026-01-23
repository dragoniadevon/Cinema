using System.ComponentModel.DataAnnotations;

namespace Cinema.Web.Models;

public class CreateMovieViewModel
{
    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public short? Duration { get; set; }

    public decimal? Rating { get; set; }

    // 👇 ТІЛЬКИ ID жанрів
    public List<int> SelectedGenres { get; set; } = new();
}
