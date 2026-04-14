using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public AttendanceController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var attendance = _context.Attendance
            .Include(x => x.Student).ThenInclude(x => x!.Group)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Subject)
            .Include(x => x.GroupSubject).ThenInclude(x => x!.Teacher)
            .AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            attendance = attendance.Where(x => x.GroupSubject!.TeacherId == _currentUser.TeacherId.Value);
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            attendance = attendance.Where(x => x.StudentId == _currentUser.StudentId.Value);
        }

        return View(await attendance.OrderByDescending(x => x.LessonDate).ToListAsync());
    }

    [Authorize(Roles = "Admin,Teacher")]
    public IActionResult Create()
    {
        LoadLists();
        return View(new Attendance { LessonDate = DateTime.Today, IsPresent = true });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Attendance attendance)
    {
        if (!await CanEditAttendanceAsync(attendance.StudentId, attendance.GroupSubjectId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadLists(attendance.StudentId, attendance.GroupSubjectId);
            return View(attendance);
        }

        _context.Attendance.Add(attendance);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Attendance), attendance.Id, $"Добавлена посещаемость за {attendance.LessonDate:d}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var attendance = await _context.Attendance.FindAsync(id);
        if (attendance == null || !await CanEditAttendanceAsync(attendance.StudentId, attendance.GroupSubjectId))
        {
            return NotFound();
        }

        LoadLists(attendance.StudentId, attendance.GroupSubjectId);
        return View(attendance);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Attendance attendance)
    {
        if (id != attendance.Id)
        {
            return NotFound();
        }

        if (!await CanEditAttendanceAsync(attendance.StudentId, attendance.GroupSubjectId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadLists(attendance.StudentId, attendance.GroupSubjectId);
            return View(attendance);
        }

        _context.Update(attendance);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Attendance), attendance.Id, $"Изменена посещаемость за {attendance.LessonDate:d}");
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanEditAttendanceAsync(int studentId, int groupSubjectId)
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
        ViewBag.StudentId = new SelectList(_context.Students.OrderBy(x => x.LastName).ToList(), "Id", "FullName", studentId);

        var groupSubjects = _context.GroupSubjects
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Semester)
            .AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            groupSubjects = groupSubjects.Where(x => x.TeacherId == _currentUser.TeacherId.Value);
        }

        ViewBag.GroupSubjectId = new SelectList(groupSubjects.ToList().Select(x => new
        {
            x.Id,
            Name = $"{x.Group?.Name} - {x.Subject?.Name} - {x.Semester?.Name}"
        }), "Id", "Name", groupSubjectId);
    }
}
