using System.Security.Claims;
using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using DeanOfficeCourseWork.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly VerificationCodeService _verificationCodes;
    private readonly NotificationService _notifications;
    private readonly AuditService _audit;

    public ProfileController(ApplicationDbContext context, IWebHostEnvironment environment, VerificationCodeService verificationCodes, NotificationService notifications, AuditService audit)
    {
        _context = context;
        _environment = environment;
        _verificationCodes = verificationCodes;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        return user == null ? RedirectToAction("Login", "Account") : View(ToViewModel(user));
    }

    public async Task<IActionResult> Avatar()
    {
        var user = await GetCurrentUserAsync();
        if (!string.IsNullOrWhiteSpace(user?.ProfilePhotoPath))
        {
            return Redirect(user.ProfilePhotoPath);
        }

        const string placeholder = """
<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 96 96">
  <rect width="96" height="96" rx="48" fill="#e4e9e7"/>
  <circle cx="48" cy="35" r="17" fill="#8a9692"/>
  <path d="M20 84c4-18 17-28 28-28s24 10 28 28" fill="#8a9692"/>
</svg>
""";

        return File(System.Text.Encoding.UTF8.GetBytes(placeholder), "image/svg+xml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePersonalData(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!PhoneCountryCatalog.TryBuildInternationalNumber(model.PhoneCountryCode, model.PhoneLocalNumber, out var normalizedPhoneNumber, out var phoneError))
        {
            ModelState.AddModelError(nameof(ProfileViewModel.PhoneLocalNumber), phoneError!);
        }

        if (!ModelState.IsValid)
        {
            var current = ToViewModel(user);
            current.Email = model.Email;
            current.PhoneCountryCode = model.PhoneCountryCode;
            current.PhoneLocalNumber = model.PhoneLocalNumber;
            current.PhoneNumber = normalizedPhoneNumber ?? model.PhoneNumber;
            return View("Index", current);
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.EmailConfirmed = false;
        }

        if (!string.Equals(user.PhoneNumber, normalizedPhoneNumber, StringComparison.OrdinalIgnoreCase))
        {
            user.PhoneNumberConfirmed = false;
        }

        user.Email = model.Email;
        user.PhoneNumber = normalizedPhoneNumber;

        if (user.Teacher != null && !string.IsNullOrWhiteSpace(model.Email))
        {
            user.Teacher.Email = model.Email;
        }

        await _context.SaveChangesAsync();
        await _audit.LogAsync("UpdateProfile", nameof(User), user.Id, "Обновлены личные данные");
        TempData["ProfileMessage"] = "Личные данные сохранены.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailCode()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["ProfileError"] = "Сначала укажите почту.";
            return RedirectToAction(nameof(Index));
        }

        VerificationCode code;
        try
        {
            code = await _verificationCodes.CreateAsync(user.Id, VerificationCodeService.EmailConfirmation, user.Email);
            await _notifications.SendEmailCodeAsync(user.Email, code.Code, "Подтверждение почты");
        }
        catch (Exception ex)
        {
            TempData["ProfileError"] = $"Не удалось отправить код на почту: {ex.Message}";
            await _audit.LogAsync("SendVerificationCodeFailed", nameof(User), user.Id, "Не удалось отправить код подтверждения почты");
            return RedirectToAction(nameof(Index));
        }

        await _audit.LogAsync("SendVerificationCode", nameof(VerificationCode), code.Id, "Отправлен код подтверждения почты через SMTP");
        TempData["EmailCodeId"] = code.Id;
        TempData["ProfileMessage"] = $"Код отправлен на {VerificationCodeService.MaskDestination(user.Email)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || !model.EmailCodeId.HasValue || string.IsNullOrWhiteSpace(model.EmailCode))
        {
            TempData["ProfileError"] = "Введите шестизначный код подтверждения почты.";
            return RedirectToAction(nameof(Index));
        }

        var code = await _verificationCodes.FindValidAsync(model.EmailCodeId.Value, model.EmailCode, VerificationCodeService.EmailConfirmation);
        if (code == null || code.UserId != user.Id || code.Destination != user.Email)
        {
            TempData["EmailCodeId"] = model.EmailCodeId;
            TempData["ProfileError"] = "Код подтверждения почты неверный или истек.";
            await _audit.LogAsync("ConfirmEmailFailed", nameof(User), user.Id, "Введен неверный или просроченный код подтверждения почты");
            return RedirectToAction(nameof(Index));
        }

        code.UsedAt = DateTime.UtcNow;
        user.EmailConfirmed = true;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("ConfirmEmail", nameof(User), user.Id, "Почта подтверждена");
        TempData["ProfileMessage"] = "Почта подтверждена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPhoneCode()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            TempData["ProfileError"] = "Сначала укажите номер телефона.";
            return RedirectToAction(nameof(Index));
        }

        VerificationCode code;
        string deliveryMessage;
        try
        {
            code = await _verificationCodes.CreateAsync(user.Id, VerificationCodeService.PhoneConfirmation, user.PhoneNumber);
            deliveryMessage = await _notifications.SendSmsCodeAsync(user.PhoneNumber, code.Code);
        }
        catch (Exception ex)
        {
            TempData["ProfileError"] = $"Не удалось отправить код на телефон: {ex.Message}";
            await _audit.LogAsync("SendVerificationCodeFailed", nameof(User), user.Id, "Не удалось отправить код подтверждения телефона");
            return RedirectToAction(nameof(Index));
        }

        await _audit.LogAsync("SendVerificationCode", nameof(VerificationCode), code.Id, "Сформирован код подтверждения телефона через SMS-провайдер");
        TempData["PhoneCodeId"] = code.Id;
        TempData["ProfileMessage"] = deliveryMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPhone(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || !model.PhoneCodeId.HasValue || string.IsNullOrWhiteSpace(model.PhoneCode))
        {
            TempData["ProfileError"] = "Введите шестизначный код подтверждения телефона.";
            return RedirectToAction(nameof(Index));
        }

        var code = await _verificationCodes.FindValidAsync(model.PhoneCodeId.Value, model.PhoneCode, VerificationCodeService.PhoneConfirmation);
        if (code == null || code.UserId != user.Id || code.Destination != user.PhoneNumber)
        {
            TempData["PhoneCodeId"] = model.PhoneCodeId;
            TempData["ProfileError"] = "Код подтверждения телефона неверный или истек.";
            await _audit.LogAsync("ConfirmPhoneFailed", nameof(User), user.Id, "Введен неверный или просроченный код подтверждения телефона");
            return RedirectToAction(nameof(Index));
        }

        code.UsedAt = DateTime.UtcNow;
        user.PhoneNumberConfirmed = true;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("ConfirmPhone", nameof(User), user.Id, "Телефон подтвержден");
        TempData["ProfileMessage"] = "Телефон подтвержден.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(IFormFile? photo, string? croppedPhotoData)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(uploadDirectory);

        if (!string.IsNullOrWhiteSpace(croppedPhotoData))
        {
            const string prefix = "data:image/png;base64,";
            if (!croppedPhotoData.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ProfileError"] = "Не удалось обработать отредактированное фото.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = Convert.FromBase64String(croppedPhotoData[prefix.Length..]);
            var croppedFileName = $"user_{user.Id}.png";
            var croppedFullPath = Path.Combine(uploadDirectory, croppedFileName);

            await System.IO.File.WriteAllBytesAsync(croppedFullPath, bytes);

            user.ProfilePhotoPath = $"/uploads/profiles/{croppedFileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            await _context.SaveChangesAsync();
            await _audit.LogAsync("UploadProfilePhoto", nameof(User), user.Id, "Обновлено и обрезано фото профиля");
            TempData["ProfileMessage"] = "Фото профиля обновлено.";
            return RedirectToAction(nameof(Index));
        }

        if (photo == null || photo.Length == 0)
        {
            TempData["ProfileError"] = "Выберите файл фото.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowed.Contains(extension))
        {
            TempData["ProfileError"] = "Разрешены только JPG, PNG или GIF.";
            return RedirectToAction(nameof(Index));
        }

        var fileName = $"user_{user.Id}{extension}";
        var fullPath = Path.Combine(uploadDirectory, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await photo.CopyToAsync(stream);
        }

        user.ProfilePhotoPath = $"/uploads/profiles/{fileName}";
        await _context.SaveChangesAsync();
        await _audit.LogAsync("UploadProfilePhoto", nameof(User), user.Id, "Обновлено фото профиля");
        TempData["ProfileMessage"] = "Фото профиля обновлено.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue("UserId");
        return int.TryParse(userIdValue, out var userId)
            ? await _context.Users.Include(x => x.Teacher).FirstOrDefaultAsync(x => x.Id == userId)
            : null;
    }

    private static ProfileViewModel ToViewModel(User user)
    {
        var phoneCountry = PhoneCountryCatalog.FindByPhoneNumber(user.PhoneNumber);
        return new ProfileViewModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            PhoneCountryCode = phoneCountry.CountryCode,
            PhoneLocalNumber = PhoneCountryCatalog.ExtractLocalNumber(user.PhoneNumber, phoneCountry),
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            ProfilePhotoPath = user.ProfilePhotoPath
        };
    }
}
