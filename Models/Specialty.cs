using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class Specialty
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public int FacultyId { get; set; }
    public Faculty? Faculty { get; set; }

    public List<Group> Groups { get; set; } = new();
}
