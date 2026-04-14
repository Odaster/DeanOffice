using System.Text;
using System.Net;
using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Services;
using DeanOfficeCourseWork.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;

    public ReportsController(ApplicationDbContext context, CurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> AverageGrades()
    {
        return View(await GetAverageGradesAsync());
    }

    public async Task<IActionResult> Debtors()
    {
        return View(await GetDebtorsAsync());
    }

    public async Task<IActionResult> AttendanceSummary()
    {
        return View(await GetAttendanceSummaryAsync());
    }

    public async Task<IActionResult> ExportAverageGrades(string format = "csv")
    {
        var rows = await GetAverageGradesAsync();
        if (format.Equals("doc", StringComparison.OrdinalIgnoreCase))
        {
            var wordContent = ToWordDocument(
                "Средний балл студентов",
                new[] { "Студент", "Группа", "Средний балл", "Количество оценок" },
                rows.Select(x => new[] { x.StudentName, x.GroupName, x.AverageGrade.ToString("F2"), x.GradesCount.ToString() }));
            return ExportWord(wordContent, "average_grades");
        }

        var content = ToDelimited("Студент;Группа;Средний балл;Количество оценок", rows.Select(x => $"{x.StudentName};{x.GroupName};{x.AverageGrade:F2};{x.GradesCount}"));
        return Export(content, "average_grades", format);
    }

    public async Task<IActionResult> ExportDebtors(string format = "csv")
    {
        var rows = await GetDebtorsAsync();
        if (format.Equals("doc", StringComparison.OrdinalIgnoreCase))
        {
            var wordContent = ToWordDocument(
                "Студенты с задолженностями",
                new[] { "Студент", "Группа", "Дисциплина", "Оценка" },
                rows.Select(x => new[] { x.StudentName, x.GroupName, x.SubjectName, x.GradeValue.ToString() }));
            return ExportWord(wordContent, "debtors");
        }

        var content = ToDelimited("Студент;Группа;Дисциплина;Оценка", rows.Select(x => $"{x.StudentName};{x.GroupName};{x.SubjectName};{x.GradeValue}"));
        return Export(content, "debtors", format);
    }

    public async Task<IActionResult> ExportAttendance(string format = "csv")
    {
        var rows = await GetAttendanceSummaryAsync();
        if (format.Equals("doc", StringComparison.OrdinalIgnoreCase))
        {
            var wordContent = ToWordDocument(
                "Посещаемость по группам и дисциплинам",
                new[] { "Группа", "Дисциплина", "Всего занятий", "Посещено", "Процент" },
                rows.Select(x => new[] { x.GroupName, x.SubjectName, x.TotalLessons.ToString(), x.PresentCount.ToString(), x.AttendancePercent.ToString("F2") }));
            return ExportWord(wordContent, "attendance");
        }

        var content = ToDelimited("Группа;Дисциплина;Всего занятий;Посещено;Процент", rows.Select(x => $"{x.GroupName};{x.SubjectName};{x.TotalLessons};{x.PresentCount};{x.AttendancePercent:F2}"));
        return Export(content, "attendance", format);
    }

    private async Task<List<ReportAverageGradeViewModel>> GetAverageGradesAsync()
    {
        var grades = _context.Grades
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.GroupSubject)
            .AsQueryable();

        grades = ApplyGradeAccess(grades);

        return await grades
            .GroupBy(x => new { x.StudentId, x.Student!.LastName, x.Student.FirstName, x.Student.MiddleName, GroupName = x.Student.Group!.Name })
            .Select(g => new ReportAverageGradeViewModel
            {
                StudentId = g.Key.StudentId,
                StudentName = (g.Key.LastName + " " + g.Key.FirstName + " " + g.Key.MiddleName).Trim(),
                GroupName = g.Key.GroupName,
                AverageGrade = g.Average(x => x.Value),
                GradesCount = g.Count()
            })
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.StudentName)
            .ToListAsync();
    }

    private async Task<List<ReportDebtorViewModel>> GetDebtorsAsync()
    {
        var grades = _context.Grades
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Subject)
            .Where(x => x.Value < 4);

        grades = ApplyGradeAccess(grades);

        return await grades
            .Select(x => new ReportDebtorViewModel
            {
                StudentId = x.StudentId,
                StudentName = (x.Student!.LastName + " " + x.Student.FirstName + " " + x.Student.MiddleName).Trim(),
                GroupName = x.Student.Group!.Name,
                SubjectName = x.GroupSubject!.Subject!.Name,
                GradeValue = x.Value
            })
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.StudentName)
            .ToListAsync();
    }

    private async Task<List<ReportAttendanceViewModel>> GetAttendanceSummaryAsync()
    {
        var attendance = _context.Attendance
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Group)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Subject)
            .AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            attendance = attendance.Where(x => x.GroupSubject!.TeacherId == _currentUser.TeacherId.Value);
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            attendance = attendance.Where(x => x.StudentId == _currentUser.StudentId.Value);
        }

        return await attendance
            .GroupBy(x => new { GroupName = x.GroupSubject!.Group!.Name, SubjectName = x.GroupSubject.Subject!.Name })
            .Select(g => new ReportAttendanceViewModel
            {
                GroupName = g.Key.GroupName,
                SubjectName = g.Key.SubjectName,
                TotalLessons = g.Count(),
                PresentCount = g.Count(x => x.IsPresent),
                AttendancePercent = g.Count() == 0 ? 0 : g.Count(x => x.IsPresent) * 100.0 / g.Count()
            })
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.SubjectName)
            .ToListAsync();
    }

    private IQueryable<Models.Grade> ApplyGradeAccess(IQueryable<Models.Grade> grades)
    {
        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            grades = grades.Where(x => x.GroupSubject!.TeacherId == _currentUser.TeacherId.Value);
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            grades = grades.Where(x => x.StudentId == _currentUser.StudentId.Value);
        }

        return grades;
    }

    private static string ToDelimited(string header, IEnumerable<string> rows)
    {
        var builder = new StringBuilder();
        builder.Append('\uFEFF');
        builder.AppendLine(header);
        foreach (var row in rows)
        {
            builder.AppendLine(row);
        }

        return builder.ToString();
    }

    private FileContentResult Export(string content, string name, string format)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        if (format.Equals("xls", StringComparison.OrdinalIgnoreCase))
        {
            return File(bytes, "application/vnd.ms-excel", $"{name}.xls");
        }

        return File(bytes, "text/csv", $"{name}.csv");
    }

    private static string ToWordDocument(string title, string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.Append('\uFEFF');
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html><head><meta charset=\"utf-8\">");
        builder.AppendLine("<style>body{font-family:Arial,sans-serif;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #333;padding:6px;} th{background:#eeeeee;}</style>");
        builder.AppendLine("</head><body>");
        builder.AppendLine($"<h1>{WebUtility.HtmlEncode(title)}</h1>");
        builder.AppendLine("<table><thead><tr>");
        foreach (var header in headers)
        {
            builder.AppendLine($"<th>{WebUtility.HtmlEncode(header)}</th>");
        }
        builder.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            foreach (var value in row)
            {
                builder.AppendLine($"<td>{WebUtility.HtmlEncode(value)}</td>");
            }
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private FileContentResult ExportWord(string content, string name)
    {
        return File(Encoding.UTF8.GetBytes(content), "application/msword", $"{name}.doc");
    }
}
