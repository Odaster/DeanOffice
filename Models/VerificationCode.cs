using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class VerificationCode
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(40)]
    public string Purpose { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Destination { get; set; } = string.Empty;

    [Required, MaxLength(6)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    [MaxLength(80)]
    public string? ResetToken { get; set; }
}
