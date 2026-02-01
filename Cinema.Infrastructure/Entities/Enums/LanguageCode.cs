namespace Cinema.Infrastructure.Entities.Enums;

using System.ComponentModel.DataAnnotations;

public enum LanguageCode : short
{
    [Display(Name = "Українська")]
    UA,

    [Display(Name = "Англійська")]
    EN,

    [Display(Name = "Польська")]
    PL,

    [Display(Name = "Німецька")]
    DE,

    [Display(Name = "Французька")]
    FR
}
