using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Group
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 6)]
    public int Course { get; set; }

    public int YearOfAdmission { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public List<Student> Students { get; set; } = new();
    public List<GroupSubject> GroupSubjects { get; set; } = new();
}
