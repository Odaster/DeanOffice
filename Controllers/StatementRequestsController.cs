using System.Net;
using System.Text;
using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using DeanOfficeCourseWork.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class StatementRequestsController : Controller
{
    private static readonly string[] CertificatePurposes = ["По месту требования", "На работу", "В налоговую", "В военкомат"];
    private static readonly string[] AbsenceReasons = ["Уважительная", "Неуважительная"];
    private static readonly string[] Statuses = [StatementRequest.StatusPending, StatementRequest.StatusRejected, StatementRequest.StatusPrinted];

    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public StatementRequestsController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        await EnsureTableAsync();

        var requests = _context.StatementRequests
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .AsQueryable();

        if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            requests = requests.Where(x => x.StudentId == _currentUser.StudentId.Value);
        }
        else if (!_currentUser.IsAdmin)
        {
            return Forbid();
        }

        return View(await requests.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create()
    {
        await EnsureTableAsync();

        if (!_currentUser.StudentId.HasValue)
        {
            return Forbid();
        }

        return View(await BuildCreateModelAsync(_currentUser.StudentId.Value, new StatementRequestCreateViewModel()));
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StatementRequestCreateViewModel model)
    {
        await EnsureTableAsync();

        if (!_currentUser.StudentId.HasValue)
        {
            return Forbid();
        }

        await ValidateCreateModelAsync(model, _currentUser.StudentId.Value);
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateModelAsync(_currentUser.StudentId.Value, model));
        }

        var request = new StatementRequest
        {
            StudentId = _currentUser.StudentId.Value,
            RequestType = model.RequestType,
            Status = StatementRequest.StatusPending,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (model.RequestType == StatementRequest.TypeEducationCertificate)
        {
            request.CertificateWithEmblem = model.CertificateWithEmblem;
            request.CertificatePurpose = model.CertificatePurpose;
        }
        else
        {
            request.SubjectId = model.SubjectId;
            request.TeacherId = model.TeacherId;
            request.MissedDate = model.MissedDate;
            request.AbsenceReason = model.AbsenceReason;
        }

        _context.StatementRequests.Add(request);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("CreateStatementRequest", nameof(StatementRequest), request.Id, $"Создана заявка: {request.RequestType}");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        await EnsureTableAsync();
        var request = await FindRequestAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        await LoadEditViewDataAsync(request);
        return View(request);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StatementRequest input)
    {
        await EnsureTableAsync();
        var request = await _context.StatementRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        if (!Statuses.Contains(input.Status))
        {
            ModelState.AddModelError(nameof(StatementRequest.Status), "Выберите корректный статус.");
        }

        await ValidateEditModelAsync(input, request.StudentId);

        if (!ModelState.IsValid)
        {
            var fullRequest = await FindRequestAsync(id) ?? input;
            fullRequest.Status = input.Status;
            fullRequest.AdminComment = input.AdminComment;
            fullRequest.CertificateWithEmblem = input.CertificateWithEmblem;
            fullRequest.CertificatePurpose = input.CertificatePurpose;
            fullRequest.SubjectId = input.SubjectId;
            fullRequest.TeacherId = input.TeacherId;
            fullRequest.MissedDate = input.MissedDate;
            fullRequest.AbsenceReason = input.AbsenceReason;
            await LoadEditViewDataAsync(fullRequest);
            return View(fullRequest);
        }

        request.Status = input.Status;
        request.AdminComment = input.AdminComment;
        request.CertificateWithEmblem = input.CertificateWithEmblem;
        request.CertificatePurpose = input.CertificatePurpose;
        request.SubjectId = input.SubjectId;
        request.TeacherId = input.TeacherId;
        request.MissedDate = input.MissedDate;
        request.AbsenceReason = input.AbsenceReason;
        request.UpdatedAt = DateTime.Now;
        request.PrintedAt = input.Status == StatementRequest.StatusPrinted ? DateTime.Now : request.PrintedAt;

        await _context.SaveChangesAsync();
        await _audit.LogAsync("UpdateStatementRequest", nameof(StatementRequest), request.Id, $"Обновлена заявка, статус: {request.Status}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Print(int id)
    {
        var request = await MarkPrintedAndFindAsync(id);
        return request == null ? NotFound() : View(request);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportWord(int id)
    {
        var request = await MarkPrintedAndFindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        var html = BuildWordDocument(request);
        return File(Encoding.UTF8.GetBytes(html), "application/msword", $"statement_{request.Id}.doc");
    }

    private async Task<StatementRequest?> MarkPrintedAndFindAsync(int id)
    {
        await EnsureTableAsync();
        var request = await FindRequestAsync(id);
        if (request == null)
        {
            return null;
        }

        request.Status = StatementRequest.StatusPrinted;
        request.PrintedAt = DateTime.Now;
        request.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("PrintStatementRequest", nameof(StatementRequest), request.Id, "Заявка отправлена на печать");
        return request;
    }

    private async Task<StatementRequest?> FindRequestAsync(int id)
    {
        return await _context.StatementRequests
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task LoadEditViewDataAsync(StatementRequest request)
    {
        ViewBag.Statuses = Statuses;
        ViewBag.CertificatePurposes = CertificatePurposes;
        ViewBag.AbsenceReasons = AbsenceReasons;

        var groupId = request.Student?.GroupId
            ?? await _context.Students.Where(x => x.Id == request.StudentId).Select(x => x.GroupId).FirstAsync();

        var options = await _context.GroupSubjects
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Subject!.Name)
            .ThenBy(x => x.Teacher!.LastName)
            .ToListAsync();

        ViewBag.Subjects = options
            .GroupBy(x => new { x.SubjectId, x.Subject!.Name })
            .Select(x => new StatementSubjectOption { Id = x.Key.SubjectId, Name = x.Key.Name })
            .ToList();

        ViewBag.Teachers = options
            .Select(x => new StatementTeacherOption { Id = x.TeacherId, SubjectId = x.SubjectId, Name = x.Teacher!.FullName })
            .DistinctBy(x => new { x.Id, x.SubjectId })
            .ToList();
    }

    private async Task<StatementRequestCreateViewModel> BuildCreateModelAsync(int studentId, StatementRequestCreateViewModel model)
    {
        var groupId = await _context.Students
            .Where(x => x.Id == studentId)
            .Select(x => x.GroupId)
            .FirstAsync();

        var options = await _context.GroupSubjects
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Subject!.Name)
            .ThenBy(x => x.Teacher!.LastName)
            .ToListAsync();

        model.Subjects = options
            .GroupBy(x => new { x.SubjectId, x.Subject!.Name })
            .Select(x => new StatementSubjectOption { Id = x.Key.SubjectId, Name = x.Key.Name })
            .ToList();
        model.Teachers = options
            .Select(x => new StatementTeacherOption { Id = x.TeacherId, SubjectId = x.SubjectId, Name = x.Teacher!.FullName })
            .DistinctBy(x => new { x.Id, x.SubjectId })
            .ToList();

        return model;
    }

    private async Task ValidateCreateModelAsync(StatementRequestCreateViewModel model, int studentId)
    {
        if (model.RequestType == StatementRequest.TypeEducationCertificate)
        {
            if (string.IsNullOrWhiteSpace(model.CertificatePurpose) || !CertificatePurposes.Contains(model.CertificatePurpose))
            {
                ModelState.AddModelError(nameof(model.CertificatePurpose), "Выберите назначение справки.");
            }

            return;
        }

        if (model.RequestType != StatementRequest.TypeRetake)
        {
            ModelState.AddModelError(nameof(model.RequestType), "Выберите корректный тип ведомостички.");
            return;
        }

        if (!model.SubjectId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SubjectId), "Выберите предмет.");
        }

        if (!model.TeacherId.HasValue)
        {
            ModelState.AddModelError(nameof(model.TeacherId), "Выберите преподавателя.");
        }

        if (!model.MissedDate.HasValue)
        {
            ModelState.AddModelError(nameof(model.MissedDate), "Выберите дату пропуска.");
        }

        if (string.IsNullOrWhiteSpace(model.AbsenceReason) || !AbsenceReasons.Contains(model.AbsenceReason))
        {
            ModelState.AddModelError(nameof(model.AbsenceReason), "Выберите причину пропуска.");
        }

        if (model.SubjectId.HasValue && model.TeacherId.HasValue)
        {
            var studentGroupId = await _context.Students.Where(x => x.Id == studentId).Select(x => x.GroupId).FirstAsync();
            var isValidPair = await _context.GroupSubjects.AnyAsync(x =>
                x.GroupId == studentGroupId &&
                x.SubjectId == model.SubjectId.Value &&
                x.TeacherId == model.TeacherId.Value);

            if (!isValidPair)
            {
                ModelState.AddModelError(nameof(model.TeacherId), "Выберите преподавателя, который ведет выбранный предмет у вашей группы.");
            }
        }
    }

    private async Task ValidateEditModelAsync(StatementRequest input, int studentId)
    {
        if (input.RequestType == StatementRequest.TypeEducationCertificate)
        {
            if (string.IsNullOrWhiteSpace(input.CertificatePurpose) || !CertificatePurposes.Contains(input.CertificatePurpose))
            {
                ModelState.AddModelError(nameof(StatementRequest.CertificatePurpose), "Выберите назначение справки.");
            }

            return;
        }

        if (input.RequestType != StatementRequest.TypeRetake)
        {
            ModelState.AddModelError(nameof(StatementRequest.RequestType), "Выберите корректный тип ведомостички.");
            return;
        }

        if (!input.SubjectId.HasValue)
        {
            ModelState.AddModelError(nameof(StatementRequest.SubjectId), "Выберите предмет.");
        }

        if (!input.TeacherId.HasValue)
        {
            ModelState.AddModelError(nameof(StatementRequest.TeacherId), "Выберите преподавателя.");
        }

        if (!input.MissedDate.HasValue)
        {
            ModelState.AddModelError(nameof(StatementRequest.MissedDate), "Выберите дату пропуска.");
        }

        if (string.IsNullOrWhiteSpace(input.AbsenceReason) || !AbsenceReasons.Contains(input.AbsenceReason))
        {
            ModelState.AddModelError(nameof(StatementRequest.AbsenceReason), "Выберите причину пропуска.");
        }

        if (input.SubjectId.HasValue && input.TeacherId.HasValue)
        {
            var studentGroupId = await _context.Students.Where(x => x.Id == studentId).Select(x => x.GroupId).FirstAsync();
            var isValidPair = await _context.GroupSubjects.AnyAsync(x =>
                x.GroupId == studentGroupId &&
                x.SubjectId == input.SubjectId.Value &&
                x.TeacherId == input.TeacherId.Value);

            if (!isValidPair)
            {
                ModelState.AddModelError(nameof(StatementRequest.TeacherId), "Выберите преподавателя, который ведет выбранный предмет у группы студента.");
            }
        }
    }

    private async Task EnsureTableAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS "StatementRequests" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "StudentId" integer NOT NULL REFERENCES "Students"("Id") ON DELETE CASCADE,
    "RequestType" varchar(40) NOT NULL,
    "Status" varchar(40) NOT NULL,
    "CertificateWithEmblem" boolean NOT NULL DEFAULT false,
    "CertificatePurpose" varchar(80),
    "SubjectId" integer REFERENCES "Subjects"("Id") ON DELETE SET NULL,
    "TeacherId" integer REFERENCES "Teachers"("Id") ON DELETE SET NULL,
    "MissedDate" timestamp without time zone,
    "AbsenceReason" varchar(40),
    "AdminComment" varchar(500),
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PrintedAt" timestamp without time zone
);
""");
    }

    private static string BuildWordDocument(StatementRequest request)
    {
        var builder = new StringBuilder();
        builder.Append('\uFEFF');
        builder.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        builder.AppendLine("<style>body{font-family:Arial,sans-serif;font-size:14px;} .doc{max-width:760px;margin:0 auto;} h1{text-align:center;font-size:22px;} p{line-height:1.5;} .sign{margin-top:48px;}</style>");
        builder.AppendLine("</head><body><div class=\"doc\">");
        builder.AppendLine($"<h1>{WebUtility.HtmlEncode(request.RequestType)}</h1>");
        builder.AppendLine($"<p><strong>Студент:</strong> {WebUtility.HtmlEncode(request.Student?.FullName)}</p>");
        builder.AppendLine($"<p><strong>Группа:</strong> {WebUtility.HtmlEncode(request.Student?.Group?.Name)}</p>");
        builder.AppendLine($"<p><strong>Дата заявки:</strong> {request.CreatedAt:dd.MM.yyyy}</p>");

        if (request.RequestType == StatementRequest.TypeEducationCertificate)
        {
            builder.AppendLine($"<p><strong>Справка с гербом:</strong> {(request.CertificateWithEmblem ? "Да" : "Нет")}</p>");
            builder.AppendLine($"<p><strong>Назначение:</strong> {WebUtility.HtmlEncode(request.CertificatePurpose)}</p>");
            builder.AppendLine("<p>Настоящая справка подтверждает, что студент обучается в учебном заведении.</p>");
        }
        else
        {
            builder.AppendLine($"<p><strong>Предмет:</strong> {WebUtility.HtmlEncode(request.Subject?.Name)}</p>");
            builder.AppendLine($"<p><strong>Преподаватель:</strong> {WebUtility.HtmlEncode(request.Teacher?.FullName)}</p>");
            builder.AppendLine($"<p><strong>Дата пропуска:</strong> {request.MissedDate:dd.MM.yyyy}</p>");
            builder.AppendLine($"<p><strong>Причина:</strong> {WebUtility.HtmlEncode(request.AbsenceReason)}</p>");
        }

        if (!string.IsNullOrWhiteSpace(request.AdminComment))
        {
            builder.AppendLine($"<p><strong>Комментарий администратора:</strong> {WebUtility.HtmlEncode(request.AdminComment)}</p>");
        }

        builder.AppendLine("<p class=\"sign\">Ответственный сотрудник деканата: ____________________</p>");
        builder.AppendLine("</div></body></html>");
        return builder.ToString();
    }
}
