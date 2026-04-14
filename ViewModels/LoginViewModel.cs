using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Логин")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;
}
