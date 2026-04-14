using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class SubjectsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public SubjectsController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var subjects = _context.Subjects.AsQueryable();
        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            var teacherId = _currentUser.TeacherId.Value;
            subjects = subjects.Where(x => x.GroupSubjects.Any(gs => gs.TeacherId == teacherId));
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            var studentId = _currentUser.StudentId.Value;
            subjects = subjects.Where(x => x.GroupSubjects.Any(gs => gs.Group!.Students.Any(s => s.Id == studentId)));
        }

        return View(await subjects.OrderBy(x => x.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var subject = await _context.Subjects.FirstOrDefaultAsync(x => x.Id == id);
        return subject == null ? NotFound() : View(subject);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Subject());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subject subject)
    {
        if (!ModelState.IsValid)
        {
            return View(subject);
        }

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Subject), subject.Id, $"Создана дисциплина {subject.Name}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        return subject == null ? NotFound() : View(subject);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Subject subject)
    {
        if (id != subject.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(subject);
        }

        _context.Update(subject);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Subject), subject.Id, $"Изменена дисциплина {subject.Name}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _context.Subjects.FirstOrDefaultAsync(x => x.Id == id);
        return subject == null ? NotFound() : View(subject);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject != null)
        {
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(Subject), id, $"Удалена дисциплина {subject.Name}");
        }

        return RedirectToAction(nameof(Index));
    }
}
