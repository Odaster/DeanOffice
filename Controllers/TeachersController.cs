using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class TeachersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public TeachersController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var teachers = _context.Teachers.AsQueryable();
        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            teachers = teachers.Where(x => x.Id == _currentUser.TeacherId.Value);
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            var studentId = _currentUser.StudentId.Value;
            teachers = teachers.Where(x => x.GroupSubjects.Any(gs => gs.Group!.Students.Any(s => s.Id == studentId)));
        }

        return View(await teachers.OrderBy(x => x.LastName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(x => x.Id == id);
        return teacher == null ? NotFound() : View(teacher);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Teacher());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Teacher teacher)
    {
        if (!ModelState.IsValid)
        {
            return View(teacher);
        }

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Teacher), teacher.Id, $"Создан преподаватель {teacher.FullName}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        return teacher == null ? NotFound() : View(teacher);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Teacher teacher)
    {
        if (id != teacher.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(teacher);
        }

        _context.Update(teacher);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Teacher), teacher.Id, $"Изменен преподаватель {teacher.FullName}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(x => x.Id == id);
        return teacher == null ? NotFound() : View(teacher);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher != null)
        {
            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(Teacher), id, $"Удален преподаватель {teacher.FullName}");
        }

        return RedirectToAction(nameof(Index));
    }
}
