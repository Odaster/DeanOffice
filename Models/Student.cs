using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Student
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(30)]
    public string StudentBookNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public User? User { get; set; }
    public List<Grade> Grades { get; set; } = new();
    public List<Attendance> AttendanceRecords { get; set; } = new();

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
}
