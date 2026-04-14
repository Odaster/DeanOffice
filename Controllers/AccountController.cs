using System.Security.Claims;
using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using DeanOfficeCourseWork.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeanOfficeCourseWork.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly VerificationCodeService _verificationCodes;
    private readonly NotificationService _notifications;
    private static readonly TimeSpan PasswordResetResendDelay = TimeSpan.FromMinutes(1);

    public AccountController(ApplicationDbContext context, AuditService audit, VerificationCodeService verificationCodes, NotificationService notifications)
    {
        _context = context;
        _audit = audit;
        _verificationCodes = verificationCodes;
        _notifications = notifications;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        User? user;
        try
        {
            user = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserName == model.UserName && x.PasswordHash == model.Password);
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            ModelState.AddModelError(string.Empty, "Нет подключения к PostgreSQL. Проверьте, что сервер запущен, база DeanOfficeDb создана, а строка подключения в appsettings.json верная.");
            return View(model);
        }

        if (user == null || user.Role == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return View(model);
        }

        await SignInUserAsync(user);

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var contact = model.Contact.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == contact || x.PhoneNumber == contact);

        if (user == null)
        {
            await _audit.LogAsync(null, "PasswordResetAccountNotFound", nameof(User), contact, "Попытка восстановления пароля по неизвестной почте или телефону");
            ModelState.AddModelError(nameof(model.Contact), "Учетная запись с такой почтой или телефоном не найдена.");
            return View(model);
        }

        VerificationCode code;
        string deliveryMessage;
        try
        {
            code = await _verificationCodes.CreateAsync(user.Id, VerificationCodeService.PasswordReset, contact);
            if (contact.Contains('@'))
            {
                await _notifications.SendEmailCodeAsync(contact, code.Code, "Восстановление пароля");
                deliveryMessage = $"Код отправлен на {VerificationCodeService.MaskDestination(contact)}.";
            }
            else
            {
                deliveryMessage = await _notifications.SendSmsCodeAsync(contact, code.Code);
            }
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(user.Id, "PasswordResetCodeSendFailed", nameof(User), user.Id, "Не удалось отправить код восстановления пароля");
            ModelState.AddModelError(nameof(model.Contact), $"Не удалось отправить код: {ex.Message}");
            return View(model);
        }

        await _audit.LogAsync(user.Id, "PasswordResetCodeSent", nameof(VerificationCode), code.Id, "Отправлен или сформирован код восстановления пароля");
        TempData["ResetMessage"] = deliveryMessage;
        return RedirectToAction(nameof(VerifyResetCode), new { id = code.Id });
    }

    [AllowAnonymous]
    public async Task<IActionResult> VerifyResetCode(int id)
    {
        var code = await _context.VerificationCodes.FirstOrDefaultAsync(x => x.Id == id && x.Purpose == VerificationCodeService.PasswordReset);
        if (code == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(ToVerifyResetCodeViewModel(code));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyResetCode(VerifyResetCodeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var currentCode = await _context.VerificationCodes.FirstOrDefaultAsync(x => x.Id == model.VerificationCodeId);
            if (currentCode != null)
            {
                model.Destination = VerificationCodeService.MaskDestination(currentCode.Destination);
                model.ResendAvailableAtUnixMilliseconds = GetResendAvailableAtUnixMilliseconds(currentCode);
            }

            return View(model);
        }

        var code = await _verificationCodes.FindValidAsync(model.VerificationCodeId, model.Code, VerificationCodeService.PasswordReset);
        if (code == null)
        {
            await _audit.LogAsync(null, "PasswordResetInvalidCode", nameof(VerificationCode), model.VerificationCodeId, "Введен неверный или просроченный код восстановления пароля");
            ModelState.AddModelError(nameof(model.Code), "Код неверный или истек.");
            var currentCode = await _context.VerificationCodes.FirstOrDefaultAsync(x => x.Id == model.VerificationCodeId);
            if (currentCode != null)
            {
                model.Destination = VerificationCodeService.MaskDestination(currentCode.Destination);
                model.ResendAvailableAtUnixMilliseconds = GetResendAvailableAtUnixMilliseconds(currentCode);
            }

            return View(model);
        }

        code.UsedAt = DateTime.UtcNow;
        code.ResetToken = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();
        await _audit.LogAsync(code.UserId, "PasswordResetCodeConfirmed", nameof(VerificationCode), code.Id, "Код восстановления пароля подтвержден");

        return RedirectToAction(nameof(ResetPassword), new { token = code.ResetToken });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendResetCode(int id)
    {
        var oldCode = await _context.VerificationCodes
            .FirstOrDefaultAsync(x => x.Id == id && x.Purpose == VerificationCodeService.PasswordReset);
        if (oldCode == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        var resendAvailableAt = oldCode.CreatedAt.Add(PasswordResetResendDelay);
        if (DateTime.UtcNow < resendAvailableAt)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((resendAvailableAt - DateTime.UtcNow).TotalSeconds));
            TempData["ResetMessage"] = $"Повторная отправка будет доступна через {seconds} сек.";
            await _audit.LogAsync(oldCode.UserId, "PasswordResetCodeResendTooEarly", nameof(VerificationCode), oldCode.Id, "Попытка повторной отправки кода восстановления пароля раньше таймера");
            return RedirectToAction(nameof(VerifyResetCode), new { id = oldCode.Id });
        }

        VerificationCode newCode;
        string deliveryMessage;
        try
        {
            newCode = await _verificationCodes.CreateAsync(oldCode.UserId, VerificationCodeService.PasswordReset, oldCode.Destination);
            if (oldCode.Destination.Contains('@'))
            {
                await _notifications.SendEmailCodeAsync(oldCode.Destination, newCode.Code, "Восстановление пароля");
                deliveryMessage = $"Новый код отправлен на {VerificationCodeService.MaskDestination(oldCode.Destination)}.";
            }
            else
            {
                deliveryMessage = await _notifications.SendSmsCodeAsync(oldCode.Destination, newCode.Code);
            }
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(oldCode.UserId, "PasswordResetCodeResendFailed", nameof(VerificationCode), oldCode.Id, "Не удалось повторно отправить код восстановления пароля");
            TempData["ResetError"] = $"Не удалось отправить новый код: {ex.Message}";
            return RedirectToAction(nameof(VerifyResetCode), new { id = oldCode.Id });
        }

        await _audit.LogAsync(newCode.UserId, "PasswordResetCodeResent", nameof(VerificationCode), newCode.Id, "Повторно отправлен или сформирован код восстановления пароля");
        TempData["ResetMessage"] = deliveryMessage;
        return RedirectToAction(nameof(VerifyResetCode), new { id = newCode.Id });
    }

    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string token)
    {
        var code = await FindResetTokenAsync(token);
        if (code == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = await FindResetTokenAsync(model.Token);
        if (code?.User == null)
        {
            await _audit.LogAsync(null, "PasswordResetInvalidToken", nameof(VerificationCode), "unknown", "Попытка сброса пароля по недействительной ссылке");
            ModelState.AddModelError(string.Empty, "Ссылка сброса пароля недействительна или истекла.");
            return View(model);
        }

        code.User.PasswordHash = model.NewPassword;
        code.ResetToken = null;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(code.UserId, "PasswordResetCompleted", nameof(User), code.UserId, "Пароль восстановлен через код подтверждения");

        var user = await _context.Users.Include(x => x.Role).FirstAsync(x => x.Id == code.UserId);
        await SignInUserAsync(user);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdValue = User.FindFirstValue("UserId");
        if (!int.TryParse(userIdValue, out var userId))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        if (user.PasswordHash != model.CurrentPassword)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Текущий пароль указан неверно.");
            return View(model);
        }

        if (model.NewPassword == model.CurrentPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Новый пароль должен отличаться от текущего.");
            return View(model);
        }

        user.PasswordHash = model.NewPassword;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("ChangePassword", nameof(User), user.Id, $"Пользователь {user.UserName} сменил пароль");

        TempData["SuccessMessage"] = "Пароль успешно изменен.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static bool IsDatabaseConnectionError(Exception ex)
    {
        return ex is NpgsqlException ||
               ex.InnerException is NpgsqlException ||
               ex is InvalidOperationException && ex.InnerException is NpgsqlException;
    }

    private async Task<VerificationCode?> FindResetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _context.VerificationCodes
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Purpose == VerificationCodeService.PasswordReset &&
                x.ResetToken == token &&
                x.UsedAt != null &&
                x.ExpiresAt > DateTime.UtcNow);
    }

    private static VerifyResetCodeViewModel ToVerifyResetCodeViewModel(VerificationCode code)
    {
        return new VerifyResetCodeViewModel
        {
            VerificationCodeId = code.Id,
            Destination = VerificationCodeService.MaskDestination(code.Destination),
            ResendAvailableAtUnixMilliseconds = GetResendAvailableAtUnixMilliseconds(code)
        };
    }

    private static long GetResendAvailableAtUnixMilliseconds(VerificationCode code)
    {
        var resendAvailableAt = DateTime.SpecifyKind(code.CreatedAt, DateTimeKind.Utc).Add(PasswordResetResendDelay);
        return new DateTimeOffset(resendAvailableAt).ToUnixTimeMilliseconds();
    }

    private async Task SignInUserAsync(User user)
    {
        if (user.Role == null)
        {
            await _context.Entry(user).Reference(x => x.Role).LoadAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role!.Name),
            new("UserId", user.Id.ToString())
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));
        }

        if (user.TeacherId.HasValue)
        {
            claims.Add(new Claim("TeacherId", user.TeacherId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
