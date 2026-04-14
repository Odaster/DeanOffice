using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Subject
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Hours { get; set; }

    public List<GroupSubject> GroupSubjects { get; set; } = new();
}
