using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Attendance
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int GroupSubjectId { get; set; }
    public GroupSubject? GroupSubject { get; set; }

    [DataType(DataType.Date)]
    public DateTime LessonDate { get; set; } = DateTime.Today;

    public bool IsPresent { get; set; } = true;

    [MaxLength(300)]
    public string? Comment { get; set; }
}
