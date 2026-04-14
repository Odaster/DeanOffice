using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public StudentsController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var students = _context.Students.Include(x => x.Group).ThenInclude(x => x!.Specialty).AsQueryable();

        if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            students = students.Where(x => x.Id == _currentUser.StudentId.Value);
        }
        else if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            var teacherId = _currentUser.TeacherId.Value;
            students = students.Where(x => x.Group!.GroupSubjects.Any(gs => gs.TeacherId == teacherId));
        }

        return View(await students.OrderBy(x => x.LastName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await _context.Students.Include(x => x.Group).ThenInclude(x => x!.Specialty).FirstOrDefaultAsync(x => x.Id == id);
        if (student == null || !await CanViewStudentAsync(student.Id))
        {
            return NotFound();
        }

        return View(student);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        LoadGroups();
        return View(new Student { BirthDate = DateTime.Today.AddYears(-18), IsActive = true });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student)
    {
        if (!ModelState.IsValid)
        {
            LoadGroups(student.GroupId);
            return View(student);
        }

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Student), student.Id, $"Создан студент {student.FullName}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        LoadGroups(student.GroupId);
        return View(student);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student student)
    {
        if (id != student.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            LoadGroups(student.GroupId);
            return View(student);
        }

        _context.Update(student);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Student), student.Id, $"Изменен студент {student.FullName}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students.Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == id);
        return student == null ? NotFound() : View(student);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(Student), id, $"Удален студент {student.FullName}");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanViewStudentAsync(int studentId)
    {
        if (_currentUser.IsAdmin)
        {
            return true;
        }

        if (_currentUser.IsStudent)
        {
            return _currentUser.StudentId == studentId;
        }

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            var teacherId = _currentUser.TeacherId.Value;
            return await _context.Students.AnyAsync(x => x.Id == studentId && x.Group!.GroupSubjects.Any(gs => gs.TeacherId == teacherId));
        }

        return false;
    }

    private void LoadGroups(int? selectedId = null)
    {
        ViewBag.GroupId = new SelectList(_context.Groups.OrderBy(x => x.Name), "Id", "Name", selectedId);
    }
}
