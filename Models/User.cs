using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(120), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30), Phone]
    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    [MaxLength(300)]
    public string? ProfilePhotoPath { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int? StudentId { get; set; }
    public Student? Student { get; set; }

    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public List<AuditLog> AuditLogs { get; set; } = new();
    public List<VerificationCode> VerificationCodes { get; set; } = new();
}
