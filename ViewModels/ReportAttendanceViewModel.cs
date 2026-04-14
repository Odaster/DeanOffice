namespace DeanOfficeCourseWork.ViewModels;

public class ReportAttendanceViewModel
{
    public string GroupName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int PresentCount { get; set; }
    public double AttendancePercent { get; set; }
}
