using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string EntityId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;
}
