using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.ViewModels;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Новый пароль должен содержать не менее 6 символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Новый пароль и подтверждение не совпадают.")]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
