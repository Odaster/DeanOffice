using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.ViewModels;

public class ProfileViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Почта")]
    public string? Email { get; set; }

    public string PhoneCountryCode { get; set; } = PhoneCountryCatalog.DefaultCountry.CountryCode;

    [Display(Name = "Номер телефона")]
    public string? PhoneLocalNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public IReadOnlyList<PhoneCountryOption> PhoneCountries => PhoneCountryCatalog.Countries;

    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? ProfilePhotoPath { get; set; }

    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("\\d{6}", ErrorMessage = "Код должен состоять из 6 цифр.")]
    public string? EmailCode { get; set; }

    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("\\d{6}", ErrorMessage = "Код должен состоять из 6 цифр.")]
    public string? PhoneCode { get; set; }

    public int? EmailCodeId { get; set; }
    public int? PhoneCodeId { get; set; }
}
