namespace DeanOfficeCourseWork.ViewModels;

public class ReportAverageGradeViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public double AverageGrade { get; set; }
    public int GradesCount { get; set; }
}
