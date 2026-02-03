using System.ComponentModel.DataAnnotations;

namespace Cinema.Web.Models.Account;

public class RegisterVm
{
    [Required]
    [Display(Name = "Імʼя")]
    public string FirstName { get; set; } = null!;

    [Required]
    [Display(Name = "Прізвище")]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Підтвердження пароля")]
    public string ConfirmPassword { get; set; } = null!;
}
