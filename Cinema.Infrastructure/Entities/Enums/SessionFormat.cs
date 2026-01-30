namespace Cinema.Infrastructure.Entities.Enums;

using System.ComponentModel.DataAnnotations;

public enum SessionFormat
{
    [Display(Name = "2D")]
    TwoD = 1,

    [Display(Name = "3D")]
    ThreeD = 2,

    [Display(Name = "IMAX")]
    IMAX = 3
}

