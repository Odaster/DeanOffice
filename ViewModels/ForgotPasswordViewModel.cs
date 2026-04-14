using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [Display(Name = "Почта или номер телефона")]
    public string Contact { get; set; } = string.Empty;
}
