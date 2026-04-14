using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Teacher
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    public User? User { get; set; }
    public List<GroupSubject> GroupSubjects { get; set; } = new();

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
}
