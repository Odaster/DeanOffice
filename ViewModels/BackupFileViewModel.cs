namespace DeanOfficeCourseWork.ViewModels;

public class BackupFileViewModel
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
