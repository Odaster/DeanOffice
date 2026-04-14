using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Semester
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Number { get; set; }

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public List<GroupSubject> GroupSubjects { get; set; } = new();
}
