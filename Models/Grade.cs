using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Grade
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int GroupSubjectId { get; set; }
    public GroupSubject? GroupSubject { get; set; }

    [Range(0, 10)]
    public int Value { get; set; }

    [Required, MaxLength(40)]
    public string GradeType { get; set; } = "Экзамен";

    [DataType(DataType.Date)]
    public DateTime GradeDate { get; set; } = DateTime.Today;

    [MaxLength(300)]
    public string? Comment { get; set; }
}
