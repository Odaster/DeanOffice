using System.ComponentModel.DataAnnotations;
using DeanOfficeCourseWork.Models;

namespace DeanOfficeCourseWork.ViewModels;

public class StatementRequestCreateViewModel
{
    [Required]
    [Display(Name = "Тип ведомостички")]
    public string RequestType { get; set; } = StatementRequest.TypeEducationCertificate;

    [Display(Name = "Справка с гербом")]
    public bool CertificateWithEmblem { get; set; }

    [Display(Name = "Назначение")]
    public string? CertificatePurpose { get; set; } = "По месту требования";

    [Display(Name = "Предмет")]
    public int? SubjectId { get; set; }

    [Display(Name = "Преподаватель")]
    public int? TeacherId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Дата пропуска")]
    public DateTime? MissedDate { get; set; } = DateTime.Today;

    [Display(Name = "Причина")]
    public string? AbsenceReason { get; set; } = "Уважительная";

    public List<StatementSubjectOption> Subjects { get; set; } = new();
    public List<StatementTeacherOption> Teachers { get; set; } = new();
}

public class StatementSubjectOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StatementTeacherOption
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
}
