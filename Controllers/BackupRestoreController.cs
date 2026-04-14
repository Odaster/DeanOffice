using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeanOfficeCourseWork.Controllers;

[Authorize(Roles = "Admin")]
public class BackupRestoreController : Controller
{
    private readonly BackupService _backupService;
    private readonly AuditService _audit;

    public BackupRestoreController(BackupService backupService, AuditService audit)
    {
        _backupService = backupService;
        _audit = audit;
    }

    public IActionResult Index(string? message = null, string? error = null)
    {
        ViewBag.Message = message;
        ViewBag.Error = error;
        ViewBag.IsWindowsServer = OperatingSystem.IsWindows();
        return View(_backupService.GetBackups());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            var fileName = await _backupService.CreateBackupAsync();
            await _audit.LogAsync("Backup", "Database", fileName, "Создана резервная копия базы данных");
            return RedirectToAction(nameof(Index), new { message = $"Создана резервная копия {fileName}" });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Index), new { error = $"Не удалось создать резервную копию: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string fileName)
    {
        try
        {
            await _backupService.RestoreAsync(fileName);
            await _audit.LogAsync("Restore", "Database", fileName, "Выполнено восстановление базы данных");
            return RedirectToAction(nameof(Index), new { message = $"Выполнено восстановление из {fileName}" });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Index), new { error = $"Не удалось восстановить базу: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateScheduledBackup(string startTime)
    {
        try
        {
            var taskName = await _backupService.CreateScheduledBackupTaskAsync(startTime);
            await _audit.LogAsync("CreateScheduledBackupTask", "Database", taskName, $"Создана задача планового резервного копирования каждые 4 часа, начиная с {startTime}");
            return RedirectToAction(nameof(Index), new { message = $"Плановое резервное копирование добавлено в Windows Task Scheduler: {taskName}, каждые 4 часа, начиная с {startTime}." });
        }
        catch (Exception ex)
        {
            await _audit.LogAsync("CreateScheduledBackupTaskFailed", "Database", "Windows Task Scheduler", "Не удалось создать задачу планового резервного копирования");
            return RedirectToAction(nameof(Index), new { error = $"Не удалось добавить плановое резервное копирование: {ex.Message}" });
        }
    }
}
