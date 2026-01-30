namespace Cinema.Infrastructure.Entities.Enums;

using System.ComponentModel.DataAnnotations;

public enum AgeRating : short
{
    [Display(Name = "0+")]
    G = 0,      // для всіх
    [Display(Name = "6+")]
    PG = 6,
    [Display(Name = "12+")]
    PG13 = 13,

    [Display(Name = "16+")]
    R = 16,
    [Display(Name = "18+")]
    NC17 = 18
}