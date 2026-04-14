using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize]
public class GroupsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly AuditService _audit;

    public GroupsController(ApplicationDbContext context, CurrentUserService currentUser, AuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var groups = _context.Groups.Include(x => x.Specialty).ThenInclude(x => x!.Faculty).AsQueryable();

        if (_currentUser.IsTeacher && _currentUser.TeacherId.HasValue)
        {
            var teacherId = _currentUser.TeacherId.Value;
            groups = groups.Where(x => x.GroupSubjects.Any(gs => gs.TeacherId == teacherId));
        }
        else if (_currentUser.IsStudent && _currentUser.StudentId.HasValue)
        {
            groups = groups.Where(x => x.Students.Any(s => s.Id == _currentUser.StudentId.Value));
        }

        return View(await groups.OrderBy(x => x.Name).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudentsNextCourse()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("CALL promote_students_next_course();");
            await _audit.LogAsync("PromoteStudentsNextCourse", nameof(Group), "all", "Выполнен перевод активных групп на следующий курс через процедуру promote_students_next_course");
            TempData["SuccessMessage"] = "Активные группы переведены на следующий курс.";
        }
        catch (Exception ex)
        {
            await _audit.LogAsync("PromoteStudentsNextCourseFailed", nameof(Group), "all", "Не удалось выполнить процедуру перевода активных групп на следующий курс");
            TempData["ErrorMessage"] = $"Не удалось выполнить перевод: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var group = await _context.Groups.Include(x => x.Specialty).ThenInclude(x => x!.Faculty).Include(x => x.Students).FirstOrDefaultAsync(x => x.Id == id);
        return group == null ? NotFound() : View(group);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        LoadSpecialties();
        return View(new Group { Course = 1, YearOfAdmission = DateTime.Today.Year });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Group group)
    {
        if (!ModelState.IsValid)
        {
            LoadSpecialties(group.SpecialtyId);
            return View(group);
        }

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(Group), group.Id, $"Создана группа {group.Name}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        LoadSpecialties(group.SpecialtyId);
        return View(group);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Group group)
    {
        if (id != group.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            LoadSpecialties(group.SpecialtyId);
            return View(group);
        }

        _context.Update(group);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(Group), group.Id, $"Изменена группа {group.Name}");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _context.Groups.Include(x => x.Specialty).FirstOrDefaultAsync(x => x.Id == id);
        return group == null ? NotFound() : View(group);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(Group), id, $"Удалена группа {group.Name}");
        }

        return RedirectToAction(nameof(Index));
    }

    private void LoadSpecialties(int? selectedId = null)
    {
        ViewBag.SpecialtyId = new SelectList(_context.Specialties.OrderBy(x => x.Name), "Id", "Name", selectedId);
    }
}
