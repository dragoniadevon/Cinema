using System.ComponentModel.DataAnnotations;
using Cinema.Infrastructure.Entities.Enums;

namespace Cinema.Web.Models;

public class CreateMovieViewModel
{
    [Required]
    [Display(Name = "Назва")]
    public string Title { get; set; } = null!;

    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Display(Name = "Тривалість (хв)")]
    [Range(1, 1000, ErrorMessage = "Тривалість має бути більшою за 0")]
    public short? Duration { get; set; }

    [Display(Name = "Дата релізу")]
    public DateOnly? ReleaseDate { get; set; }

    [Display(Name = "Рейтинг")]
    [Range(0, 10)]
    public decimal? Rating { get; set; }

    [Display(Name = "Постер (URL)")]
    [Url]
    public string? PosterUrl { get; set; }

    [Display(Name = "Трейлер (URL)")]
    [Url]
    public string? TrailerUrl { get; set; }

    // 🔽 ENUM-и
    [Display(Name = "Віковий рейтинг")]
    public AgeRating? AgeRating { get; set; }

    [Display(Name = "Мова")]
    public LanguageCode? LanguageCode { get; set; }

    [Display(Name = "Країна")]
    public CountryCode? CountryCode { get; set; }

    // 🔽 жанри
    public List<int> SelectedGenres { get; set; } = new();

    // 🔽 актори
    public List<int> SelectedActors { get; set; } = new();

}
