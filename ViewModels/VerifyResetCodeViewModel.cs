using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.ViewModels;

public class VerifyResetCodeViewModel
{
    public int VerificationCodeId { get; set; }
    public string Destination { get; set; } = string.Empty;
    public long ResendAvailableAtUnixMilliseconds { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("\\d{6}", ErrorMessage = "Код должен состоять из 6 цифр.")]
    [Display(Name = "Шестизначный код")]
    public string Code { get; set; } = string.Empty;
}
