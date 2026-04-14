using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using DeanOfficeCourseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;

    public UsersController(ApplicationDbContext context, AuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Student)
            .Include(x => x.Teacher)
            .OrderBy(x => x.UserName)
            .ToListAsync();

        return View(users);
    }

    public IActionResult Create()
    {
        LoadLists();
        return View(new User());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        await NormalizeUserRoleLinksAsync(user);
        await ValidateUserLinksAsync(user);

        if (!ModelState.IsValid)
        {
            LoadLists(user.RoleId, user.StudentId, user.TeacherId);
            return View(user);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(User), user.Id, $"Создан пользователь {user.UserName}");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        LoadLists(user.RoleId, user.StudentId, user.TeacherId);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        await NormalizeUserRoleLinksAsync(user);
        await ValidateUserLinksAsync(user);

        if (!ModelState.IsValid)
        {
            LoadLists(user.RoleId, user.StudentId, user.TeacherId);
            return View(user);
        }

        _context.Update(user);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", nameof(User), user.Id, $"Изменен пользователь {user.UserName}");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
        return user == null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", nameof(User), id, $"Удален пользователь {user.UserName}");
        }

        return RedirectToAction(nameof(Index));
    }

    private void LoadLists(int? roleId = null, int? studentId = null, int? teacherId = null)
    {
        ViewBag.RoleId = new SelectList(_context.Roles.OrderBy(x => x.Name), "Id", "Name", roleId);
        ViewBag.StudentId = new SelectList(
            _context.Students
                .Where(x => x.User == null || x.Id == studentId)
                .OrderBy(x => x.LastName)
                .ToList(),
            "Id",
            "FullName",
            studentId);
        ViewBag.TeacherId = new SelectList(
            _context.Teachers
                .Where(x => x.User == null || x.Id == teacherId)
                .OrderBy(x => x.LastName)
                .ToList(),
            "Id",
            "FullName",
            teacherId);
    }

    private async Task ValidateUserLinksAsync(User user)
    {
        if (await _context.Users.AnyAsync(x => x.UserName == user.UserName && x.Id != user.Id))
        {
            ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.UserName), "Пользователь с таким логином уже существует.");
        }

        if (user.StudentId.HasValue &&
            await _context.Users.AnyAsync(x => x.StudentId == user.StudentId && x.Id != user.Id))
        {
            ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.StudentId), "Этот студент уже привязан к другой учетной записи.");
        }

        if (user.TeacherId.HasValue &&
            await _context.Users.AnyAsync(x => x.TeacherId == user.TeacherId && x.Id != user.Id))
        {
            ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.TeacherId), "Этот преподаватель уже привязан к другой учетной записи.");
        }
    }

    private async Task NormalizeUserRoleLinksAsync(User user)
    {
        var roleName = await _context.Roles
            .Where(x => x.Id == user.RoleId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();

        if (roleName == "Student")
        {
            user.TeacherId = null;
            if (!user.StudentId.HasValue)
            {
                ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.StudentId), "Для роли Student выберите студента.");
                return;
            }

            var studentFullName = await _context.Students
                .Where(x => x.Id == user.StudentId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync();
            if (studentFullName == null)
            {
                ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.StudentId), "Выбранный студент не найден.");
                return;
            }

            user.FullName = studentFullName;
            ModelState.Remove(nameof(DeanOfficeCourseWork.Models.User.FullName));
        }
        else if (roleName == "Teacher")
        {
            user.StudentId = null;
            if (!user.TeacherId.HasValue)
            {
                ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.TeacherId), "Для роли Teacher выберите преподавателя.");
                return;
            }

            var teacherFullName = await _context.Teachers
                .Where(x => x.Id == user.TeacherId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync();
            if (teacherFullName == null)
            {
                ModelState.AddModelError(nameof(DeanOfficeCourseWork.Models.User.TeacherId), "Выбранный преподаватель не найден.");
                return;
            }

            user.FullName = teacherFullName;
            ModelState.Remove(nameof(DeanOfficeCourseWork.Models.User.FullName));
        }
        else
        {
            user.StudentId = null;
            user.TeacherId = null;
        }
    }
}
