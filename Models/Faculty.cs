using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Faculty
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public List<Specialty> Specialties { get; set; } = new();
}
