namespace Cinema.Infrastructure.Entities.Enums;

using System.ComponentModel.DataAnnotations;

public enum CountryCode : short
{
    [Display(Name = "Україна")]
    UA,

    [Display(Name = "США")]
    US,

    [Display(Name = "Велика Британія")]
    GB,

    [Display(Name = "Франція")]
    FR,

    [Display(Name = "Німеччина")]
    DE,

    [Display(Name = "Польща")]
    PL
}
