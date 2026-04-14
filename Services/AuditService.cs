using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;

namespace DeanOfficeCourseWork.Services;

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly CurrentUserService _currentUser;

    public AuditService(ApplicationDbContext context, CurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string entityName, object entityId, string details)
    {
        await LogAsync(_currentUser.UserId, action, entityName, entityId, details);
    }

    public async Task LogAsync(int? userId, string action, string entityName, object entityId, string details)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId.ToString() ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Details = details
        });

        await _context.SaveChangesAsync();
    }
}
