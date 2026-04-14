namespace DeanOfficeCourseWork.Models;

public class GroupSubject
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public List<Grade> Grades { get; set; } = new();
    public List<Attendance> AttendanceRecords { get; set; } = new();
}
