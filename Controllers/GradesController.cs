using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class GradesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public GradesController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var grades = _context.Grades
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Subject)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Teacher)
            .AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            grades = grades.Where(x => x.GroupSubject!.TeacherId == _currentUser.TeacherId.Value);
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            grades = grades.Where(x => x.StudentId == _currentUser.StudentId.Value);
        }

        return View(await grades.OrderByDescending(x => x.GradeDate).ToListAsync());
    }

    [Authorize(Roles = "Admin,Teacher")]
    public IActionResult Create()
    {
        LoadLists();
        return View(new Grade { GradeDate = DateTime.Today, GradeType = "Экзамен" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Grade grade)
    {
        if (!await CanEditGradeAsync(grade.StudentId, grade.GroupSubjectId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadLists(grade.StudentId, grade.GroupSubjectId);
            return View(grade);
        }

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Grade), grade.Id, $"Добавлена оценка {grade.Value}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade == null || !await CanEditGradeAsync(grade.StudentId, grade.GroupSubjectId))
        {
            return NotFound();
        }

        LoadLists(grade.StudentId, grade.GroupSubjectId);
        return View(grade);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Grade grade)
    {
        if (id != grade.Id)
        {
            return NotFound();
        }

        if (!await CanEditGradeAsync(grade.StudentId, grade.GroupSubjectId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadLists(grade.StudentId, grade.GroupSubjectId);
            return View(grade);
        }

        _context.Update(grade);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Grade), grade.Id, $"Изменена оценка на {grade.Value}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var grade = await _context.Grades
            .Include(x => x.Student)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Subject)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (grade == null || !await CanEditGradeAsync(grade.StudentId, grade.GroupSubjectId))
        {
            return NotFound();
        }

        return View(grade);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade != null && await CanEditGradeAsync(grade.StudentId, grade.GroupSubjectId))
        {
            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(Grade), id, $"Удалена оценка {grade.Value}");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanEditGradeAsync(int studentId, int groupSubjectId)
    {
        if (_currentUser.IsAdmin)
        {
            return true;
        }

        if (!_currentUser.IsTeacher || !_currentUser.TeacherId.HasValue)
        {
            return false;
        }

        var teacherId = _currentUser.TeacherId.Value;
        return await _context.GroupSubjects.AnyAsync(x =>
            x.Id == groupSubjectId &&
            x.TeacherId == teacherId &&
            x.Group!.Students.Any(s => s.Id == studentId));
    }

    private void LoadLists(int? studentId = null, int? groupSubjectId = null)
    {
        var students = _context.Students.OrderBy(x => x.LastName).ToList();
        var groupSubjects = _context.GroupSubjects
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Semester)
            .AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            groupSubjects = groupSubjects.Where(x => x.TeacherId == _currentUser.TeacherId.Value);
        }

        ViewBag.StudentId = new SelectList(students, "Id", "FullName", studentId);
        ViewBag.GroupSubjectId = new SelectList(groupSubjects.ToList().Select(x => new
        {
            x.Id,
            Name = $"{x.Group?.Name} - {x.Subject?.Name} - {x.Semester?.Name}"
        }), "Id", "Name", groupSubjectId);
    }
}
